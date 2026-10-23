using Godot;

namespace ForagerClone.Tests.Headless;

/// <summary>
/// Headless asset audit. Loads every model and texture under <c>res://Assets</c> and prints a
/// machine-readable report (mesh/surface/vertex/bone/animation/material counts + bounds) so we
/// can plan around the real assets instead of guessing. Exits non-zero if anything fails to load.
/// Run with: tools\audit-assets.ps1
/// </summary>
public partial class AssetAudit : Node
{
    private static readonly string[] ModelExtensions = [".glb", ".fbx", ".obj"];

    public override void _Ready()
    {
        var exitCode = 1;
        try
        {
            exitCode = RunAudit();
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[audit] UNHANDLED: {exception}");
        }
        finally
        {
            // Never leave the process alive: an unhandled exception in a headless tool scene
            // otherwise hangs CI forever (see lessons_learned.md LL-014).
            GetTree().Quit(exitCode);
        }
    }

    private static int RunAudit()
    {
        var failures = 0;
        var animationNames = new SortedSet<string>(StringComparer.Ordinal);

        GD.Print("[audit] asset audit starting");

        var models = CollectFiles("res://Assets", ModelExtensions);
        GD.Print($"[audit] models found: {models.Count}");

        foreach (var path in models)
        {
            try
            {
                AuditModel(path, animationNames, ref failures);
            }
            catch (Exception exception)
            {
                GD.PrintErr($"[audit] FAILED {path}: {exception.GetType().Name}: {exception.Message}");
                failures++;
            }
        }

        var textures = CollectFiles("res://Assets", [".png"]);
        GD.Print($"[audit] png files found: {textures.Count}");

        GD.Print($"[audit] distinct animation clips ({animationNames.Count}): {string.Join(", ", animationNames)}");
        GD.Print($"[audit] SUMMARY models={models.Count} png={textures.Count} failures={failures}");

        return failures == 0 ? 0 : 1;
    }

    private static void AuditModel(string path, SortedSet<string> animationNames, ref int failures)
    {
        // Dispatch on the loaded resource type instead of the file extension: Godot imports
        // .obj as ArrayMesh, .fbx/.glb as PackedScene, and a wrong extension check crashes
        // with InvalidCastException (see lessons_learned.md LL-015). Note GetExtension()
        // returns the extension without a leading dot.
        var resource = ResourceLoader.Load(path);

        switch (resource)
        {
            case Mesh mesh:
            {
                var size = mesh.GetAabb().Size;
                GD.Print(
                    $"MODEL|{path}|kind=mesh|surfaces={mesh.GetSurfaceCount()}|" +
                    $"size={size.X:0.00}x{size.Y:0.00}x{size.Z:0.00}");
                return;
            }

            case PackedScene packed:
                AuditScene(path, packed, animationNames);
                return;

            case null:
                GD.PrintErr($"[audit] FAILED to load: {path}");
                failures++;
                return;

            default:
                GD.PrintErr($"[audit] unexpected resource type {resource.GetType().Name}: {path}");
                failures++;
                return;
        }
    }

    private static void AuditScene(string path, PackedScene packed, SortedSet<string> animationNames)
    {
        var instance = packed.Instantiate();
        try
        {
            var meshInstances = instance.FindChildren("*", "MeshInstance3D", true, false);
            var skeletons = instance.FindChildren("*", "Skeleton3D", true, false);
            var animationPlayers = instance.FindChildren("*", "AnimationPlayer", true, false);

            var clips = new List<string>();
            foreach (var node in animationPlayers)
            {
                if (node is not AnimationPlayer player)
                {
                    continue;
                }

                foreach (var clip in player.GetAnimationList())
                {
                    var clipName = clip.ToString() ?? string.Empty;
                    if (clipName.Length == 0)
                    {
                        continue;
                    }

                    clips.Add(clipName);
                    animationNames.Add(clipName);
                }
            }

            var bones = 0;
            foreach (var node in skeletons)
            {
                if (node is Skeleton3D skeleton)
                {
                    bones += skeleton.GetBoneCount();
                }
            }

            var surfaces = 0;
            var vertices = 0;
            double sizeX = 0, sizeY = 0, sizeZ = 0;
            var materials = new HashSet<string>(StringComparer.Ordinal);

            foreach (var node in meshInstances)
            {
                if (node is not MeshInstance3D meshInstance || meshInstance.Mesh is null)
                {
                    continue;
                }

                var mesh = meshInstance.Mesh;
                surfaces += mesh.GetSurfaceCount();

                for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
                {
                    // Godot 4.7 removed Mesh.SurfaceGetArrayLen; read the vertex array instead
                    // (see lessons_learned.md LL-011).
                    var arrays = mesh.SurfaceGetArrays(surface);
                    vertices += arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length;

                    var material = mesh.SurfaceGetMaterial(surface);
                    if (material is not null)
                    {
                        materials.Add(string.IsNullOrEmpty(material.ResourceName) ? "<unnamed>" : material.ResourceName);
                    }
                }

                var meshSize = mesh.GetAabb().Size;
                sizeX = Math.Max(sizeX, meshSize.X);
                sizeY = Math.Max(sizeY, meshSize.Y);
                sizeZ = Math.Max(sizeZ, meshSize.Z);
            }

            GD.Print(
                $"MODEL|{path}|kind=scene|meshes={meshInstances.Count}|surfaces={surfaces}|vertices={vertices}|" +
                $"bones={bones}|clips={clips.Count}|materials={materials.Count}|" +
                $"size={sizeX:0.00}x{sizeY:0.00}x{sizeZ:0.00}|anim=[{string.Join(",", clips)}]");
        }
        finally
        {
            instance.Free();
        }
    }

    private static List<string> CollectFiles(string root, string[] extensions)
    {
        var results = new List<string>();
        var directory = DirAccess.Open(root);
        if (directory is null)
        {
            return results;
        }

        directory.ListDirBegin();
        while (true)
        {
            var name = directory.GetNext();
            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            if (name is "." or "..")
            {
                continue;
            }

            var path = $"{root.TrimEnd('/')}/{name}";
            if (directory.CurrentIsDir())
            {
                results.AddRange(CollectFiles(path, extensions));
                continue;
            }

            foreach (var extension in extensions)
            {
                if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(path);
                    break;
                }
            }
        }

        directory.ListDirEnd();

        results.Sort(StringComparer.Ordinal);
        return results;
    }
}

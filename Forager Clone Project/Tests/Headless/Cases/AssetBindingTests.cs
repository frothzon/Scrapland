using Forager.Core.Animation;
using ForagerClone.Player;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Guards asset scale and clip-name resolution against the real GLB (LL-013 / LL-014).</summary>
[HeadlessTest(Tags = "assets,player")]
public partial class PlayerAssetTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var host = new Node3D { Name = "AssetHost" };
        AddChild(host);

        var animator = new PlayerAnimator { Name = "ModelAnchor", ModelScale = 0.73 };
        host.AddChild(animator);
        await context.StepPhysics(1);

        context.AssertNotNull(animator.Model, "the chibi GLB instantiated");
        if (animator.Model is null)
        {
            return;
        }

        var (min, max) = ExtentY(animator.Model);
        var height = max - min;
        context.AssertInRange(height, 1.25, 1.85, $"normalised player height sits near 1.5 m (got {height:0.00} m)");

        context.AssertTrue(animator.AvailableClips.Count >= 10, $"model exposes its clips ({animator.AvailableClips.Count})");

        foreach (var clip in new[] { LogicalClip.Idle, LogicalClip.Walk, LogicalClip.Run, LogicalClip.Jump })
        {
            context.AssertTrue(animator.ResolvedClips.ContainsKey(clip), $"clip '{clip}' resolved from {animator.AvailableClips.Count} names");
        }

        context.AssertTrue(animator.Play(LogicalClip.Walk), "walk clip plays");
        context.AssertEqual(LogicalClip.Walk, animator.CurrentClip, "current clip tracked");
        if (animator.Animation is not null && animator.ResolvedClips.TryGetValue(LogicalClip.Walk, out var walkName))
        {
            context.AssertEqual(walkName, animator.Animation.CurrentAnimation.ToString(), "AnimationPlayer switched to the walk clip");
        }

        context.AssertTrue(animator.Play(LogicalClip.Run), "run clip plays");
        context.AssertEqual(LogicalClip.Run, animator.CurrentClip, "run tracked");
    }

    private static (float Min, float Max) ExtentY(Node3D model)
    {
        var min = float.MaxValue;
        var max = float.MinValue;

        foreach (var child in model.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (child is not MeshInstance3D meshInstance || meshInstance.Mesh is null)
            {
                continue;
            }

            var aabb = meshInstance.Mesh.GetAabb();
            for (var corner = 0; corner < 8; corner++)
            {
                var local = aabb.Position + new Vector3(
                    (corner & 1) == 0 ? 0 : aabb.Size.X,
                    (corner & 2) == 0 ? 0 : aabb.Size.Y,
                    (corner & 4) == 0 ? 0 : aabb.Size.Z);

                var world = meshInstance.GlobalTransform * local;
                min = Math.Min(min, world.Y);
                max = Math.Max(max, world.Y);
            }
        }

        return min > max ? (0, 0) : (min, max);
    }
}

/// <summary>NodeFactory must produce a mesh, a collider and a grounded pivot for every node type.</summary>
[HeadlessTest(Tags = "assets,world")]
public partial class NodeFactoryTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        await context.StepPhysics(1);

        foreach (var id in new[] { "birch", "rock", "bush" })
        {
            var node = fixture.SpawnNode(id, new Vector3(2, 0, 2));

            var meshInstance = node.GetNodeOrNull<MeshInstance3D>("Pivot/Mesh");
            context.AssertNotNull(meshInstance, $"{id}: mesh instance built");
            context.AssertTrue(meshInstance?.Mesh is not null, $"{id}: mesh assigned");

            var collision = node.GetNodeOrNull<CollisionShape3D>("Collision");
            context.AssertTrue(collision?.Shape is CylinderShape3D, $"{id}: cylinder collider derived from bounds");

            context.AssertEqual(4u, node.CollisionLayer, $"{id}: lives on the interactable layer");
            context.AssertEqual(id, node.Definition.Id, $"{id}: definition wired");

            if (meshInstance?.Mesh is not null)
            {
                var aabb = meshInstance.Mesh.GetAabb();
                var bottom = (meshInstance.Position.Y + aabb.Position.Y) * node.Scale.Y;
                context.AssertNear(0, bottom, 0.02, $"{id}: mesh sits on y = 0 (pivot re-centred)");
            }

            node.QueueFree();
        }
    }
}

using Forager.Core.Save;
using Godot;

namespace ForagerClone.Save;

/// <summary>Save storage in <c>user://saves</c>. The path is injectable so tests never touch the real slot.</summary>
public sealed class GodotPersistence : IPersistence
{
    public const string DefaultPath = "user://saves/slot0.json";

    public GodotPersistence(string path = DefaultPath)
    {
        Path = path;
    }

    public string Path { get; }

    public bool Exists => Godot.FileAccess.FileExists(Path);

    public void Save(string json)
    {
        var directory = Path.GetBaseDir();
        if (!string.IsNullOrEmpty(directory))
        {
            DirAccess.MakeDirRecursiveAbsolute(directory);
        }

        using var file = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushError($"GodotPersistence: cannot write '{Path}'.");
            return;
        }

        file.StoreString(json);
    }

    public bool TryLoad(out string json)
    {
        json = string.Empty;
        if (!Exists)
        {
            return false;
        }

        using var file = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return false;
        }

        json = file.GetAsText();
        return true;
    }

    public void Delete()
    {
        if (Exists)
        {
            DirAccess.RemoveAbsolute(Path);
        }
    }
}

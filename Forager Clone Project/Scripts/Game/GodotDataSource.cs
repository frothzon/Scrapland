using Forager.Core.Data;
using Godot;

namespace ForagerClone.Game;

/// <summary>
/// <see cref="IDataSource"/> over <c>res://</c>. Lets the exact same Core loaders run in the engine and
/// in xUnit tests (which point the filesystem source at the project folder).
/// </summary>
public sealed class GodotDataSource : IDataSource
{
    private readonly string _root;

    public GodotDataSource(string root = "res://")
    {
        _root = root.EndsWith('/') ? root : root + "/";
    }

    public bool Exists(string relativePath) => Godot.FileAccess.FileExists(Resolve(relativePath));

    public string ReadText(string relativePath)
    {
        var path = Resolve(relativePath);
        if (!Godot.FileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"Data file '{path}' was not found.", path);
        }

        return Godot.FileAccess.GetFileAsString(path);
    }

    public IReadOnlyList<string> ListFiles(string relativeDirectory, string searchPattern = "*")
    {
        var results = new List<string>();
        using var directory = DirAccess.Open(Resolve(relativeDirectory));
        if (directory is null)
        {
            return results;
        }

        foreach (var name in directory.GetFiles())
        {
            results.Add($"{relativeDirectory.TrimEnd('/')}/{name}");
        }

        return results;
    }

    private string Resolve(string relativePath) => _root + relativePath.Replace('\\', '/').TrimStart('/');
}

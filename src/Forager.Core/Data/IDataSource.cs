namespace Forager.Core.Data;

/// <summary>
/// Abstraction over "where content lives" so the exact same loaders run in the engine
/// (Godot <c>res://</c> via FileAccess) and in pure xUnit tests (plain filesystem).
/// </summary>
public interface IDataSource
{
    bool Exists(string relativePath);

    string ReadText(string relativePath);

    /// <summary>Returns relative paths (forward slashes) of files under <paramref name="relativeDirectory"/>.</summary>
    IReadOnlyList<string> ListFiles(string relativeDirectory, string searchPattern = "*");
}

/// <summary>Filesystem-backed source. Used by tests, which read the Godot project folder directly.</summary>
public sealed class FileSystemDataSource : IDataSource
{
    private readonly string _root;

    public FileSystemDataSource(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Root directory is required.", nameof(root));
        }

        _root = Path.GetFullPath(root);
    }

    public string Root => _root;

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    public string ReadText(string relativePath) => File.ReadAllText(Resolve(relativePath));

    public IReadOnlyList<string> ListFiles(string relativeDirectory, string searchPattern = "*")
    {
        var directory = Resolve(relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private string Resolve(string relativePath)
    {
        var combined = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return Path.GetFullPath(combined);
    }
}

/// <summary>In-memory source for tests that should not touch disk.</summary>
public sealed class InMemoryDataSource : IDataSource
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryDataSource Add(string relativePath, string content)
    {
        _files[Normalize(relativePath)] = content;
        return this;
    }

    public bool Exists(string relativePath) => _files.ContainsKey(Normalize(relativePath));

    public string ReadText(string relativePath)
    {
        if (!_files.TryGetValue(Normalize(relativePath), out var content))
        {
            throw new FileNotFoundException($"Data file '{relativePath}' was not found.", relativePath);
        }

        return content;
    }

    public IReadOnlyList<string> ListFiles(string relativeDirectory, string searchPattern = "*")
    {
        var prefix = Normalize(relativeDirectory).TrimEnd('/') + "/";
        return _files.Keys
            .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static string Normalize(string relativePath) => relativePath.Replace('\\', '/').TrimStart('/');
}

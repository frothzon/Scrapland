namespace Forager.Tests;

/// <summary>
/// Locates repository folders from the test binary location. Tests read the same
/// <c>Data/*.json</c> files the game loads, so there is exactly one source of truth.
/// </summary>
internal static class TestPaths
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    /// <summary>The Godot project folder, which is also the root of <c>res://</c>.</summary>
    public static string GodotProjectRoot => Path.Combine(RepoRoot, "Forager Clone Project");

    public static string DataDirectory => Path.Combine(GodotProjectRoot, "Data");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var hasProject = Directory.Exists(Path.Combine(directory.FullName, "Forager Clone Project"));
            var hasMarker = File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))
                            || File.Exists(Path.Combine(directory.FullName, "agents.md"))
                            || Directory.Exists(Path.Combine(directory.FullName, ".git"));
            if (hasProject && hasMarker)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the repository root above '{AppContext.BaseDirectory}'. " +
            "Expected a folder containing 'Forager Clone Project' plus Directory.Build.props/agents.md/.git.");
    }
}

namespace ForagerClone.Tests.Headless;

/// <summary>Parsed <c>--</c> user arguments for the headless runner.</summary>
public sealed class RunnerOptions
{
    public string Filter { get; private set; } = "all";

    public int Seed { get; private set; } = 1337;

    public int PerTestTimeoutMs { get; private set; } = 30_000;

    public int GlobalTimeoutMs { get; private set; } = 300_000;

    public static RunnerOptions Parse(string[] userArgs)
    {
        var options = new RunnerOptions();
        if (userArgs is null)
        {
            return options;
        }

        foreach (var argument in userArgs)
        {
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = argument.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }

            var key = argument[..separator];
            var value = argument[(separator + 1)..];

            switch (key)
            {
                case "--filter":
                    options.Filter = string.IsNullOrWhiteSpace(value) ? "all" : value;
                    break;
                case "--seed" when int.TryParse(value, out var seed):
                    options.Seed = seed;
                    break;
                case "--per-test-timeout" when int.TryParse(value, out var perTestSeconds):
                    options.PerTestTimeoutMs = Math.Max(1, perTestSeconds) * 1000;
                    break;
                case "--timeout" when int.TryParse(value, out var seconds):
                    options.GlobalTimeoutMs = Math.Max(1, seconds) * 1000;
                    break;
            }
        }

        return options;
    }

    public bool Matches(string testName, IReadOnlyList<string> tags)
    {
        if (string.IsNullOrWhiteSpace(Filter) || Filter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var token in Filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (testName.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var tag in tags)
            {
                if (tag.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

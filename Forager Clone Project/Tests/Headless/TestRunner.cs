using System.Diagnostics;
using System.Reflection;
using Godot;

namespace ForagerClone.Tests.Headless;

/// <summary>
/// Headless integration test runner. Discovers every <see cref="HeadlessTest"/> marked with
/// <see cref="HeadlessTestAttribute"/>, runs it inside the live scene tree and exits with:
///   0 = all passed, 1 = at least one failure, 2 = global timeout, 3 = not running headless,
///   4 = no tests matched the filter.
/// Invoke through tools\test.ps1 or tools\godot.ps1 (see agents.md).
/// </summary>
public partial class TestRunner : Node
{
    private const int ExitSuccess = 0;
    private const int ExitTestFailure = 1;
    private const int ExitGlobalTimeout = 2;
    private const int ExitNotHeadless = 3;
    private const int ExitNoTests = 4;

    private bool _finished;

    public override async void _Ready()
    {
        var options = RunnerOptions.Parse(OS.GetCmdlineUserArgs());
        var display = DisplayServer.GetName();
        var version = Engine.GetVersionInfo().TryGetValue("string", out var value) ? value.AsString() : "unknown";

        GD.Print($"[tests] engine={version} display={display} filter={options.Filter} seed={options.Seed}");

        if (!string.Equals(display, "headless", StringComparison.Ordinal))
        {
            GD.PrintErr("[tests] REFUSED: run this suite with --headless (agents.md testing policy).");
            Finish(ExitNotHeadless);
            return;
        }

        StartWatchdog(options.GlobalTimeoutMs);

        var tests = Discover(options);
        if (tests.Count == 0)
        {
            GD.PrintErr($"[tests] no tests matched filter '{options.Filter}'.");
            Finish(ExitNoTests);
            return;
        }

        var host = new Node { Name = "HeadlessTestHost" };
        AddChild(host);

        var passed = 0;
        foreach (var (type, tags) in tests)
        {
            var outcome = await RunTest(type, host, options);
            if (outcome)
            {
                passed++;
            }
        }

        var failed = tests.Count - passed;
        GD.Print($"[tests] SUMMARY total={tests.Count} passed={passed} failed={failed}");
        Finish(failed == 0 ? ExitSuccess : ExitTestFailure);
    }

    private static List<(Type Type, string[] Tags)> Discover(RunnerOptions options)
    {
        var discovered = new List<(Type Type, string[] Tags)>();

        foreach (var type in typeof(TestRunner).Assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !typeof(HeadlessTest).IsAssignableFrom(type))
            {
                continue;
            }

            var attribute = type.GetCustomAttribute<HeadlessTestAttribute>();
            if (attribute is null)
            {
                continue;
            }

            var tags = attribute.TagList;
            if (options.Matches(type.Name, tags))
            {
                discovered.Add((type, tags));
            }
        }

        return discovered
            .OrderBy(entry => entry.Type.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<bool> RunTest(Type type, Node host, RunnerOptions options)
    {
        HeadlessTest instance;
        try
        {
            instance = (HeadlessTest)Activator.CreateInstance(type)!;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[tests] FAIL {type.Name}: could not instantiate ({exception.Message})");
            return false;
        }

        instance.Name = type.Name;

        var context = new HeadlessTestContext(GetTree(), type.Name, options.Seed);
        var stopwatch = Stopwatch.StartNew();
        host.AddChild(instance);

        try
        {
            var runTask = instance.RunAsync(context);
            var completed = await Task.WhenAny(runTask, Task.Delay(options.PerTestTimeoutMs));
            if (!ReferenceEquals(completed, runTask))
            {
                context.Fail($"Timed out after {options.PerTestTimeoutMs} ms.");
            }
            else
            {
                await runTask;
            }
        }
        catch (Exception exception)
        {
            context.Fail(exception.ToString());
        }
        finally
        {
            stopwatch.Stop();
            instance.QueueFree();
        }

        if (context.Passed)
        {
            GD.Print($"PASS {type.Name} ({stopwatch.ElapsedMilliseconds} ms)");
        }
        else
        {
            GD.Print($"FAIL {type.Name} ({stopwatch.ElapsedMilliseconds} ms)");
            foreach (var failure in context.Failures)
            {
                GD.Print($"     - {failure}");
            }
        }

        return context.Passed;
    }

    private async void StartWatchdog(int globalTimeoutMs)
    {
        // Wall-clock guard only: semantics stay deterministic, but CI can never hang.
        await Task.Delay(globalTimeoutMs);
        if (_finished)
        {
            return;
        }

        GD.PrintErr($"[tests] GLOBAL TIMEOUT after {globalTimeoutMs} ms - forcing exit code {ExitGlobalTimeout}.");
        Finish(ExitGlobalTimeout);
    }

    private void Finish(int exitCode)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        GetTree().Quit(exitCode);
    }
}

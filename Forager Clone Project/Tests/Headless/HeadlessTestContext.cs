using Forager.Core.Randomness;
using Godot;

namespace ForagerClone.Tests.Headless;

/// <summary>
/// Per-test state: assertions, deterministic RNG and frame stepping.
/// Frame stepping is physics-frame based (never wall clock) so results are reproducible.
/// </summary>
public sealed class HeadlessTestContext
{
    private readonly SceneTree _tree;

    public HeadlessTestContext(SceneTree tree, string testName, int seed)
    {
        _tree = tree;
        TestName = testName;
        Seed = seed;
        Rng = new SeededRng(seed);
    }

    public string TestName { get; }

    public int Seed { get; }

    public SeededRng Rng { get; }

    public List<string> Failures { get; } = [];

    public bool Passed => Failures.Count == 0;

    public void Fail(string message)
    {
        Failures.Add(message);
        GD.PrintErr($"[{TestName}] FAILED: {message}");
    }

    public void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            Fail(message);
        }
    }

    public void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);

    public void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Fail($"{message} (expected: {expected}, actual: {actual})");
        }
    }

    public void AssertNotEqual<T>(T unexpected, T actual, string message)
    {
        if (EqualityComparer<T>.Default.Equals(unexpected, actual))
        {
            Fail($"{message} (value should not be {unexpected})");
        }
    }

    public void AssertNear(double expected, double actual, double tolerance, string message)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            Fail($"{message} (expected: {expected} +/- {tolerance}, actual: {actual})");
        }
    }

    public void AssertNotNull(object? value, string message) => AssertTrue(value is not null, message);

    public void AssertInRange(double value, double min, double max, string message)
        => AssertTrue(value >= min && value <= max, $"{message} (value: {value}, expected {min}..{max})");

    /// <summary>Advances the simulation by whole physics frames.</summary>
    public async Task StepPhysics(int frames)
    {
        if (frames <= 0)
        {
            return;
        }

        for (var i = 0; i < frames; i++)
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
        }
    }
}

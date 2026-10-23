using Godot;

namespace ForagerClone.Tests.Headless;

/// <summary>
/// Base class for headless integration tests. Derive, add <see cref="HeadlessTestAttribute"/>,
/// and implement <see cref="RunAsync"/>. The runner instantiates the test inside the live tree,
/// so physics, raycasts and signals behave exactly as in game.
/// </summary>
public abstract partial class HeadlessTest : Node
{
    public abstract Task RunAsync(HeadlessTestContext context);
}

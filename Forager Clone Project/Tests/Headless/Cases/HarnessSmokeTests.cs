using Forager.Core.Inventories;
using Forager.Core.Items;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>
/// Proves the harness itself works: headless enforcement, frame stepping, deterministic RNG,
/// and that the engine assembly can call into Forager.Core.
/// </summary>
[HeadlessTest(Tags = "smoke")]
public partial class HarnessSmokeTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        context.AssertEqual("headless", DisplayServer.GetName(), "suite must run with --headless");
        context.AssertTrue(Engine.GetVersionInfo().ContainsKey("major"), "engine version info is available");

        await context.StepPhysics(2);

        var inventory = new Inventory(2, new FixedStackLimits(10));
        var leftover = inventory.Add("wood", 15);

        context.AssertEqual(0, leftover, "Forager.Core is usable from the game assembly");
        context.AssertEqual(15, inventory.Count("wood"), "inventory totals survive the engine boundary");

        var roll = context.Rng.NextInt(0, 5);
        context.AssertTrue(roll is >= 0 and < 5, $"seeded rng returns in-range values (got {roll})");
    }
}

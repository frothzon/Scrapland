using Forager.Core.Gathering;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Drops are magnetised towards the player and collected into the Core inventory.</summary>
[HeadlessTest(Tags = "items,pickup")]
public partial class PickupMagnetTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var tree = fixture.SpawnNode("birch", new Vector3(0, 0, -2f));
        await context.StepPhysics(1);

        var before = fixture.Session.Profile.Inventory.Count("wood");
        tree.SpawnDrops([new HarvestDrop("wood", 2)]);

        var collected = false;
        for (var frame = 0; frame < 240 && !collected; frame++)
        {
            await context.StepPhysics(1);
            collected = fixture.Session.Profile.Inventory.Count("wood") >= before + 2;
        }

        context.AssertTrue(collected, "both wood entered the inventory");
        context.AssertTrue(
            fixture.WorldRoot.GetChildren().All(child => child.Name.ToString() != "Pickup_wood"),
            "the pickup freed itself after collecting");
    }
}

/// <summary>A felled node comes back after its data-driven respawn timer.</summary>
[HeadlessTest(Tags = "world,respawn")]
public partial class RespawnTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var bush = fixture.SpawnNode("bush", new Vector3(0, 0, -2f));
        await context.StepPhysics(1);

        fixture.Interactor.AimAt(bush.GlobalPosition + (Vector3.Up * 0.5f));
        var outcome = fixture.Interactor.TryHarvestCurrentTarget();

        context.AssertNotNull(outcome, "bush harvest produced an outcome");
        context.AssertTrue(outcome!.Felled, "the bush is felled in one swing");
        context.AssertTrue(bush.State.IsDepleted, "bush reports depleted");
        context.AssertFalse(bush.MeshNode!.Visible, "the visual is hidden while depleted");

        fixture.Session.Clock.Advance(bush.Definition.RespawnSeconds + 0.5);
        await context.StepPhysics(2);

        context.AssertFalse(bush.State.IsDepleted, "the bush respawned after its timer");
        context.AssertTrue(bush.MeshNode.Visible, "the visual came back");
        context.AssertEqual(1, bush.RespawnCount, "respawn happened exactly once");
    }
}

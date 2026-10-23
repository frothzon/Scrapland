using Forager.Core.Crafting;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Station reach is measured from the player and comes from data (craft.stationRadius).</summary>
[HeadlessTest(Tags = "crafting,station")]
public partial class StationProximityTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        fixture.SpawnStation(StationKind.Workbench, new Vector3(0, 0, -4f));
        fixture.SpawnStation(StationKind.Furnace, new Vector3(4f, 0, -4f));
        await context.StepPhysics(1);

        context.AssertTrue(fixture.Session.NearbyStation(Vector3.Zero) is null, "no station in range from the origin");

        fixture.Player.GlobalPosition = new Vector3(0, 0, -2.2f);
        var near = fixture.Session.NearbyStation(fixture.Player.GlobalPosition);
        context.AssertNotNull(near, "the workbench is in range 1.8 m away");
        context.AssertEqual(StationKind.Workbench, near!.Kind, "the workbench is the nearest station");

        fixture.Player.GlobalPosition = new Vector3(4f, 0, -1.6f);
        var nearFurnace = fixture.Session.NearbyStation(fixture.Player.GlobalPosition);
        context.AssertEqual(StationKind.Furnace, nearFurnace!.Kind, "the furnace is nearest there");
    }
}

/// <summary>E on a station opens crafting instead of swinging a tool.</summary>
[HeadlessTest(Tags = "crafting,interaction")]
public partial class StationInteractTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var workbench = fixture.SpawnStation(StationKind.Workbench, new Vector3(0, 0, -2f));
        fixture.Player.GlobalPosition = new Vector3(0, 0, -0.5f);
        await context.StepPhysics(2);

        fixture.Interactor.AimAt(workbench.GlobalPosition + (Vector3.Up * 0.4f));

        context.AssertTrue(
            fixture.Interactor.Prompt.Contains("Workbench", StringComparison.Ordinal),
            $"the HUD prompt names the station (got '{fixture.Interactor.Prompt}')");

        var interacted = fixture.Interactor.TryInteractCurrentTarget();
        context.AssertTrue(interacted, "E on a station does something");
        context.AssertEqual(StationKind.Workbench, fixture.Session.ActiveStation, "the workbench became active");
        context.AssertTrue(fixture.Session.UiOpen, "the UI opened");
        context.AssertEqual(1, workbench.OpenCount, "the station registered one open");

        fixture.Session.CloseUi();
        context.AssertFalse(fixture.Session.UiOpen, "closing clears the UI flag");
        context.AssertTrue(fixture.Session.ActiveStation is null, "closing clears the active station");
    }
}

/// <summary>A panel owns input: no movement while UiOpen, movement resumes after closing.</summary>
[HeadlessTest(Tags = "crafting,ui,player")]
public partial class UiBlocksMovementTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        await context.StepPhysics(3);

        fixture.Session.OpenCrafting(null);
        fixture.Player.ApplyMoveInput(new Vector2(0, -1), run: true);

        var start = fixture.Player.GlobalPosition;
        await context.StepPhysics(45);
        var moved = new Vector2(fixture.Player.GlobalPosition.X - start.X, fixture.Player.GlobalPosition.Z - start.Z).Length();
        context.AssertTrue(moved < 0.2, $"the player stays put while a panel is open (moved {moved:0.00} m)");

        fixture.Session.CloseUi();
        await context.StepPhysics(45);
        var after = new Vector2(fixture.Player.GlobalPosition.X - start.X, fixture.Player.GlobalPosition.Z - start.Z).Length();
        context.AssertTrue(after > 1.0, $"movement resumes after closing (moved {after:0.00} m)");
    }
}

using Forager.Core.Save;
using ForagerClone.Tests.Headless.Support;
using ForagerClone.Ui;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Save → mutate → load must restore inventory, progression, time, node state and position.</summary>
[HeadlessTest(Tags = "save")]
public partial class SaveLoadRoundTripTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var session = fixture.Session;
        var tree = fixture.SpawnNode("birch", new Vector3(0, 0, -2f));
        await context.StepPhysics(1);

        session.Persistence = new InMemoryPersistence();
        session.Profile.Inventory.Add("stone", 4);
        session.Profile.AddCoins(7);
        session.Clock.Advance(12.5);
        fixture.Player.GlobalPosition = new Vector3(1.5f, 0, 1.5f);

        fixture.Interactor.AimAt(tree.GlobalPosition + (Vector3.Up * 0.6f));
        for (var swing = 0; swing < tree.Definition.HitPoints; swing++)
        {
            session.Clock.Advance(session.Balance.Gather.SwingSeconds + 0.05);
            fixture.Interactor.TryHarvestCurrentTarget();
        }

        context.AssertTrue(tree.State.IsDepleted, "precondition: the tree is felled");

        var woodBefore = session.Profile.Inventory.Count("wood");
        var stoneBefore = session.Profile.Inventory.Count("stone");
        var coinsBefore = session.Profile.Coins;
        var clockBefore = session.Clock.TotalSeconds;

        context.AssertTrue(session.SaveNow(), "save wrote a payload");

        session.Profile.Inventory.Add("wood", 9);
        session.Profile.AddCoins(100);
        session.Clock.Advance(50);
        tree.RestoreState(tree.Definition.HitPoints, 0);
        fixture.Player.GlobalPosition = new Vector3(-5, 0, -5);

        context.AssertTrue(session.LoadNow(), "load succeeded");

        context.AssertEqual(woodBefore, session.Profile.Inventory.Count("wood"), "wood restored");
        context.AssertEqual(stoneBefore, session.Profile.Inventory.Count("stone"), "stone restored");
        context.AssertEqual(coinsBefore, session.Profile.Coins, "coins restored");
        context.AssertNear(clockBefore, session.Clock.TotalSeconds, 0.001, "sim time restored");
        context.AssertTrue(tree.State.IsDepleted, "the depleted node loads back depleted");
        context.AssertNear(1.5, fixture.Player.GlobalPosition.X, 0.02, "player position restored");
    }
}

/// <summary>The HUD mirrors the Core profile: slot counts, icons, coins and the xp bar.</summary>
[HeadlessTest(Tags = "hud")]
public partial class HudBindingTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var hud = new Hud { Name = "TestHud" };
        AddChild(hud);
        await context.StepPhysics(1);

        context.AssertTrue(hud.IsBuilt, "HUD built its tree");
        hud.Bind(fixture.Session, fixture.Interactor);
        await context.StepPhysics(1);

        context.AssertEqual(5, hud.SlotCount, "hotbar shows five slots");
        context.AssertEqual("1", hud.GetSlotText(0), "the starting axe sits in slot 0");
        context.AssertNotNull(hud.GetSlotIcon(0), "the axe icon resolved from the atlas");

        fixture.Session.Profile.Inventory.Add("wood", 7);
        await context.StepPhysics(1);

        context.AssertEqual("7", hud.GetSlotText(2), "the wood count is shown");
        context.AssertNotNull(hud.GetSlotIcon(2), "the wood icon resolved from the atlas");

        fixture.Session.Profile.AddCoins(42);
        fixture.Session.Profile.AddXp(10);
        await context.StepPhysics(1);

        context.AssertTrue(hud.CoinsText.Contains("42", StringComparison.Ordinal), $"coin label updated (got '{hud.CoinsText}')");
        context.AssertInRange(hud.XpValue, 0.45, 0.55, "xp bar shows 10/20 progress");
        context.AssertTrue(hud.LevelText.Contains("1", StringComparison.Ordinal), $"level label (got '{hud.LevelText}')");
    }
}

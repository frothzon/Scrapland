using Forager.Core.Gathering;
using Forager.Core.Items;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Auto tool selection prefers the best tier, and tiers gate harder nodes.</summary>
[HeadlessTest(Tags = "crafting,tools")]
public partial class ToolTierTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        fixture.GiveItems(("stone_axe", 1));
        await context.StepPhysics(1);

        context.AssertEqual(new ToolSpec("axe", 2), fixture.Session.EquippedTool!.Value, "the stone axe outranks the wood axe");
        context.AssertEqual("stone_axe", fixture.Session.EquippedToolId, "the equipped tool id is the stone axe");

        var gold = fixture.SpawnNode("gold_rock", new Vector3(0, 0, -2f));
        await context.StepPhysics(2);
        fixture.Interactor.AimAt(gold.GlobalPosition + (Vector3.Up * 0.5f));

        var denied = fixture.Interactor.TryHarvestCurrentTarget();
        context.AssertNotNull(denied, "harvest produced an outcome");
        context.AssertEqual(HarvestStatus.MissingTool, denied!.Status, "gold needs a tier-2 pickaxe");

        fixture.GiveItems(("stone_pickaxe", 1));
        var allowed = fixture.Interactor.TryHarvestCurrentTarget();
        context.AssertTrue(allowed!.Success, $"the stone pickaxe mines it ({allowed.Status})");
        context.AssertEqual(gold.Definition.HitPoints - 1, gold.State.HitPointsLeft, "the hit landed");
    }
}

/// <summary>Ore nodes drop their ore and the pickup reaches the inventory.</summary>
[HeadlessTest(Tags = "crafting,gather,ore")]
public partial class OreNodeTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var iron = fixture.SpawnNode("iron_rock", new Vector3(0, 0, -2f));
        await context.StepPhysics(1);
        fixture.Interactor.AimAt(iron.GlobalPosition + (Vector3.Up * 0.5f));

        for (var swing = 0; swing < iron.Definition.HitPoints; swing++)
        {
            fixture.Session.Clock.Advance(fixture.Session.Balance.Gather.SwingSeconds + 0.05);
            fixture.Interactor.TryHarvestCurrentTarget();
        }

        context.AssertTrue(iron.State.IsDepleted, "the iron vein is mined out");

        var collected = false;
        for (var frame = 0; frame < 240 && !collected; frame++)
        {
            await context.StepPhysics(1);
            collected = fixture.Session.Profile.Inventory.Count("iron_ore") > 0;
        }

        context.AssertTrue(collected, "iron ore reached the inventory");
    }
}

using Forager.Core.Gathering;
using ForagerClone.Nodes;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>The aim ray only targets nodes the player can actually reach.</summary>
[HeadlessTest(Tags = "interaction")]
public partial class AimRayTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var tree = fixture.SpawnNode("birch", new Vector3(0, 0, -2.5f));
        var farTree = fixture.SpawnNode("birch", new Vector3(0, 0, -16f));
        await context.StepPhysics(2);

        fixture.Interactor.AimAt(tree.GlobalPosition + (Vector3.Up * 0.6f));

        context.AssertNotNull(fixture.Interactor.CurrentTarget, "the ray hit something");
        context.AssertEqual(tree, fixture.Interactor.CurrentTarget as ResourceNode3D, "the ray acquired the nearby birch");

        fixture.Interactor.AimAt(farTree.GlobalPosition + (Vector3.Up * 0.6f));
        context.AssertTrue(fixture.Interactor.CurrentTarget is null, "a node beyond reach is not targetable");

        fixture.Interactor.AimAt(new Vector3(0, 40, 0));
        context.AssertTrue(fixture.Interactor.CurrentTarget is null, "aiming at the sky targets nothing");
    }
}

/// <summary>Chop a birch three times: drops must reach the inventory and xp must be awarded.</summary>
[HeadlessTest(Tags = "interaction,gather")]
public partial class HarvestTreeTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var tree = fixture.SpawnNode("birch", new Vector3(0, 0, -2f));
        await context.StepPhysics(2);

        fixture.Interactor.AimAt(tree.GlobalPosition + (Vector3.Up * 0.6f));
        context.AssertEqual(tree, fixture.Interactor.CurrentTarget as ResourceNode3D, "aim acquired the tree");

        for (var swing = 1; swing <= tree.Definition.HitPoints; swing++)
        {
            fixture.Session.Clock.Advance(fixture.Session.Balance.Gather.SwingSeconds + 0.05);
            var outcome = fixture.Interactor.TryHarvestCurrentTarget();
            context.AssertNotNull(outcome, $"swing {swing} produced an outcome");
            context.AssertTrue(outcome!.Success, $"swing {swing} landed ({outcome.Status})");
        }

        context.AssertTrue(tree.State.IsDepleted, "the tree is felled after its hit points run out");
        context.AssertEqual(tree.Definition.HitPoints, fixture.Interactor.HarvestCount, "every swing counted");
        context.AssertTrue(fixture.Session.Profile.TotalXp > 0, "felling awarded xp");

        var collected = false;
        for (var frame = 0; frame < 240 && !collected; frame++)
        {
            await context.StepPhysics(1);
            collected = fixture.Session.Profile.Inventory.Count("wood") > 0;
        }

        context.AssertTrue(collected, "the magnet pulled the wood into the inventory");
    }
}

/// <summary>Tool gating: a rock needs a pickaxe and must report why it refused.</summary>
[HeadlessTest(Tags = "interaction,gather")]
public partial class MissingToolTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var rock = fixture.SpawnNode("rock", new Vector3(0, 0, -2f));
        await context.StepPhysics(2);

        fixture.Session.Profile.Inventory.Remove("pickaxe", 1);
        context.AssertEqual("wood_axe", fixture.Session.EquippedToolId, "the axe is still equipped");

        fixture.Interactor.AimAt(rock.GlobalPosition + (Vector3.Up * 0.5f));
        var outcome = fixture.Interactor.TryHarvestCurrentTarget();

        context.AssertNotNull(outcome, "harvest produced an outcome");
        context.AssertEqual(HarvestStatus.MissingTool, outcome!.Status, "rock refused without a pickaxe");
        context.AssertEqual(rock.Definition.HitPoints, rock.State.HitPointsLeft, "the rock took no damage");
        context.AssertTrue(
            fixture.Interactor.Message.Contains("pickaxe", StringComparison.OrdinalIgnoreCase),
            $"HUD message names the tool (got '{fixture.Interactor.Message}')");
        context.AssertEqual(0, fixture.Session.Profile.Inventory.Count("stone"), "no stone gathered");
    }
}

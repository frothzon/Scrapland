using Forager.Core.Config;
using Forager.Core.Gathering;
using Forager.Core.Nodes;
using Forager.Core.Randomness;

namespace Forager.Tests;

public sealed class HarvestServiceTests
{
    private static readonly GatherTuning Tuning = new(
        RangeMeters: 12,
        SwingSeconds: 0.35,
        MagnetRadius: 3,
        MagnetSpeed: 9);

    private static NodeDefinition Tree(int hitPoints = 3, string? tool = "wood_axe", double coinChance = 0)
        => new(
            "test_tree",
            "tree",
            "Test Tree",
            "res://Assets/x.obj",
            1,
            tool,
            hitPoints,
            RespawnSeconds: 10,
            Xp: 8,
            coinChance,
            [new DropSpec("wood", 1, 2)]);

    private static HarvestService Service(int seed = 7, double swingSeconds = 0.35)
        => new(Tuning with { SwingSeconds = swingSeconds }, new SeededRng(seed));

    [Fact]
    public void MissingTool_DoesNothing()
    {
        var definition = Tree();
        var state = new ResourceNodeState(definition);

        var outcome = Service().TryHarvest(definition, state, equippedToolId: null, nowSeconds: 0);

        Assert.Equal(HarvestStatus.MissingTool, outcome.Status);
        Assert.Equal(3, state.HitPointsLeft);
        Assert.Equal(0, state.Harvests);
    }

    [Fact]
    public void WrongTool_DoesNothing()
    {
        var definition = Tree();
        var state = new ResourceNodeState(definition);

        var outcome = Service().TryHarvest(definition, state, "pickaxe", 0);

        Assert.Equal(HarvestStatus.MissingTool, outcome.Status);
        Assert.Equal(3, state.HitPointsLeft);
    }

    [Fact]
    public void CorrectTool_IsCaseInsensitive()
    {
        var definition = Tree();
        var state = new ResourceNodeState(definition);

        var outcome = Service().TryHarvest(definition, state, "WOOD_AXE", 0);

        Assert.True(outcome.Success);
        Assert.Equal(2, state.HitPointsLeft);
    }

    [Fact]
    public void ToolFreeNode_WorksWithBareHands()
    {
        var definition = Tree(hitPoints: 1, tool: null);
        var state = new ResourceNodeState(definition);

        var outcome = Service().TryHarvest(definition, state, null, 0);

        Assert.True(outcome.Success);
        Assert.True(outcome.Felled);
        Assert.NotEmpty(outcome.Drops);
    }

    [Fact]
    public void OnlyTheFellingSwingRollsDrops()
    {
        var definition = Tree();
        var state = new ResourceNodeState(definition);
        var service = Service();

        var first = service.TryHarvest(definition, state, "wood_axe", 0);
        var second = service.TryHarvest(definition, state, "wood_axe", 1);
        var third = service.TryHarvest(definition, state, "wood_axe", 2);

        Assert.True(first.Success);
        Assert.False(first.Felled);
        Assert.Empty(first.Drops);
        Assert.False(second.Felled);
        Assert.True(third.Felled);
        Assert.NotEmpty(third.Drops);
        Assert.Equal(8, third.Xp);
        Assert.True(state.IsDepleted);
    }

    [Fact]
    public void DropsStayWithinDataRange()
    {
        for (var seed = 0; seed < 40; seed++)
        {
            var definition = Tree(hitPoints: 1);
            var state = new ResourceNodeState(definition);

            var outcome = new HarvestService(Tuning, new SeededRng(seed))
                .TryHarvest(definition, state, "wood_axe", 0);

            var drop = Assert.Single(outcome.Drops);
            Assert.Equal("wood", drop.ItemId);
            Assert.InRange(drop.Amount, 1, 2);
        }
    }

    [Fact]
    public void CooldownBlocksRapidSwings()
    {
        var definition = Tree();
        var state = new ResourceNodeState(definition);
        var service = Service(swingSeconds: 0.35);

        service.TryHarvest(definition, state, "wood_axe", 0);
        var tooSoon = service.TryHarvest(definition, state, "wood_axe", 0.1);
        var allowed = service.TryHarvest(definition, state, "wood_axe", 0.36);

        Assert.Equal(HarvestStatus.OnCooldown, tooSoon.Status);
        Assert.True(allowed.Success);
        Assert.Equal(1, state.HitPointsLeft);
    }

    [Fact]
    public void DepletedNode_ReportsDepleted_UntilRespawn()
    {
        var definition = Tree(hitPoints: 1);
        var state = new ResourceNodeState(definition);
        var service = Service();

        service.TryHarvest(definition, state, "wood_axe", 0);
        var whileDepleted = service.TryHarvest(definition, state, "wood_axe", 5);
        var afterRespawn = service.TryHarvest(definition, state, "wood_axe", 10.1);

        Assert.Equal(HarvestStatus.Depleted, whileDepleted.Status);
        Assert.True(afterRespawn.Success);
        Assert.True(afterRespawn.Felled, "the respawned node has 1 hp, so this swing fells it again");
        Assert.Equal(0, state.HitPointsLeft);
    }

    [Fact]
    public void CoinChance_IsRespected()
    {
        var always = Tree(hitPoints: 1, coinChance: 1);
        var never = Tree(hitPoints: 1, coinChance: 0);

        var withCoins = Service().TryHarvest(always, new ResourceNodeState(always), "wood_axe", 0);
        var withoutCoins = Service().TryHarvest(never, new ResourceNodeState(never), "wood_axe", 0);

        Assert.Equal(1, withCoins.Coins);
        Assert.Equal(0, withoutCoins.Coins);
    }
}

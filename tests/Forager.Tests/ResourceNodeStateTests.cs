using Forager.Core.Nodes;

namespace Forager.Tests;

public sealed class ResourceNodeStateTests
{
    private static NodeDefinition Tree(int hitPoints = 3, string? toolKind = null, double respawnSeconds = 10) => new(
        "tree",
        "tree",
        "Tree",
        "res://Assets/x.obj",
        1,
        toolKind,
        1,
        hitPoints,
        respawnSeconds,
        Xp: 8,
        CoinChance: 0,
        [new DropSpec("wood", 1, 1)]);

    [Fact]
    public void NewState_IsFull()
    {
        var state = new ResourceNodeState(Tree());

        Assert.Equal(3, state.HitPointsLeft);
        Assert.False(state.IsDepleted);
        Assert.Equal(0, state.SecondsUntilRespawn(0));
        Assert.Equal(0, state.Harvests);
    }

    [Fact]
    public void ApplyHit_DecrementsAndSchedulesRespawnOnLastHit()
    {
        var state = new ResourceNodeState(Tree(hitPoints: 2, respawnSeconds: 10));

        Assert.False(state.ApplyHit(1));
        Assert.Equal(1, state.HitPointsLeft);
        Assert.False(state.IsDepleted);

        Assert.True(state.ApplyHit(2));
        Assert.True(state.IsDepleted);
        Assert.Equal(12, state.RespawnAtSeconds);
        Assert.Equal(10, state.SecondsUntilRespawn(2));
    }

    [Fact]
    public void RespawnIfDue_OnlyAfterTimer()
    {
        var state = new ResourceNodeState(Tree(hitPoints: 1, respawnSeconds: 10));
        state.ApplyHit(5);

        Assert.False(state.RespawnIfDue(14.9));
        Assert.True(state.IsDepleted);

        Assert.True(state.RespawnIfDue(15));
        Assert.False(state.IsDepleted);
        Assert.Equal(1, state.HitPointsLeft);
        Assert.Equal(0, state.RespawnAtSeconds);
        Assert.False(state.RespawnIfDue(16), "already respawned");
    }

    [Fact]
    public void Restore_ClampsHitPointsAndKeepsRespawn()
    {
        var state = ResourceNodeState.Restore(Tree(hitPoints: 3), hitPointsLeft: 99, respawnAtSeconds: 42);

        Assert.Equal(3, state.HitPointsLeft);
        Assert.False(state.IsDepleted);
        Assert.Equal(42, state.RespawnAtSeconds);

        var depleted = ResourceNodeState.Restore(Tree(hitPoints: 3), hitPointsLeft: -5, respawnAtSeconds: 42);
        Assert.True(depleted.IsDepleted);
    }
}

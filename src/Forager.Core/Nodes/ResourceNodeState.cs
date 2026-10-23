namespace Forager.Core.Nodes;

/// <summary>
/// Mutable per-instance state of a harvestable node: remaining hit points, cooldown timestamp and
/// respawn schedule. Pure data — the Godot node only mirrors it for presentation.
/// </summary>
public sealed class ResourceNodeState
{
    private ResourceNodeState(
        NodeDefinition definition,
        int hitPointsLeft,
        double respawnAtSeconds,
        double lastHarvestSeconds,
        int harvests)
    {
        Definition = definition;
        HitPointsLeft = hitPointsLeft;
        RespawnAtSeconds = respawnAtSeconds;
        LastHarvestSeconds = lastHarvestSeconds;
        Harvests = harvests;
    }

    public ResourceNodeState(NodeDefinition definition)
        : this(definition, definition.HitPoints, 0, double.NegativeInfinity, 0)
    {
    }

    public NodeDefinition Definition { get; }

    public int HitPointsLeft { get; private set; }

    public bool IsDepleted => HitPointsLeft <= 0;

    /// <summary>Sim-clock time at which a depleted node becomes harvestable again.</summary>
    public double RespawnAtSeconds { get; private set; }

    public double LastHarvestSeconds { get; private set; }

    public int Harvests { get; private set; }

    /// <summary>Records a landed hit. Returns true when this hit depleted the node.</summary>
    public bool ApplyHit(double nowSeconds)
    {
        LastHarvestSeconds = nowSeconds;
        Harvests++;

        if (HitPointsLeft > 0)
        {
            HitPointsLeft--;
        }

        if (HitPointsLeft > 0)
        {
            return false;
        }

        HitPointsLeft = 0;
        RespawnAtSeconds = nowSeconds + Definition.RespawnSeconds;
        return true;
    }

    /// <summary>Restores the node when its respawn time has passed. Returns true when it respawned.</summary>
    public bool RespawnIfDue(double nowSeconds)
    {
        if (!IsDepleted || nowSeconds < RespawnAtSeconds)
        {
            return false;
        }

        HitPointsLeft = Definition.HitPoints;
        RespawnAtSeconds = 0;
        return true;
    }

    public double SecondsUntilRespawn(double nowSeconds)
        => IsDepleted ? Math.Max(0, RespawnAtSeconds - nowSeconds) : 0;

    /// <summary>Rebuilds state from a save file.</summary>
    public static ResourceNodeState Restore(NodeDefinition definition, int hitPointsLeft, double respawnAtSeconds)
        => new(
            definition,
            Math.Clamp(hitPointsLeft, 0, definition.HitPoints),
            respawnAtSeconds,
            double.NegativeInfinity,
            0);
}

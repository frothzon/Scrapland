using Forager.Core.Config;
using Forager.Core.Nodes;
using Forager.Core.Randomness;

namespace Forager.Core.Gathering;

public enum HarvestStatus
{
    Success,
    MissingTool,
    Depleted,
    OnCooldown,
}

public readonly record struct HarvestDrop(string ItemId, int Amount);

/// <summary>Result of one swing at a resource node.</summary>
public sealed class HarvestOutcome
{
    private HarvestOutcome(HarvestStatus status, bool felled, IReadOnlyList<HarvestDrop> drops, int xp, int coins)
    {
        Status = status;
        Felled = felled;
        Drops = drops;
        Xp = xp;
        Coins = coins;
    }

    public HarvestStatus Status { get; }

    /// <summary>True when this swing broke the node, i.e. <see cref="Drops"/> were rolled.</summary>
    public bool Felled { get; }

    public IReadOnlyList<HarvestDrop> Drops { get; }

    public int Xp { get; }

    public int Coins { get; }

    public bool Success => Status == HarvestStatus.Success;

    public static HarvestOutcome Fail(HarvestStatus status)
        => new(status, false, Array.Empty<HarvestDrop>(), 0, 0);

    public static HarvestOutcome Landed(bool felled, IReadOnlyList<HarvestDrop> drops, int xp, int coins)
        => new(HarvestStatus.Success, felled, drops, xp, coins);
}

/// <summary>
/// Harvest rules: tool gating, per-swing cooldown, hit points, depletion, drop rolls, xp and coins.
/// Time is always passed in (from <c>SimClock</c>) so tests can fast-forward without waiting.
/// </summary>
public sealed class HarvestService
{
    private readonly GatherTuning _tuning;
    private readonly IRng _rng;

    public HarvestService(GatherTuning tuning, IRng rng)
    {
        _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public GatherTuning Tuning => _tuning;

    public HarvestOutcome TryHarvest(
        NodeDefinition definition,
        ResourceNodeState state,
        string? equippedToolId,
        double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);

        state.RespawnIfDue(nowSeconds);

        if (state.IsDepleted)
        {
            return HarvestOutcome.Fail(HarvestStatus.Depleted);
        }

        if (definition.RequiresTool
            && !string.Equals(definition.RequiredTool, equippedToolId, StringComparison.OrdinalIgnoreCase))
        {
            return HarvestOutcome.Fail(HarvestStatus.MissingTool);
        }

        if (nowSeconds - state.LastHarvestSeconds < _tuning.SwingSeconds)
        {
            return HarvestOutcome.Fail(HarvestStatus.OnCooldown);
        }

        var felled = state.ApplyHit(nowSeconds);
        if (!felled)
        {
            return HarvestOutcome.Landed(false, Array.Empty<HarvestDrop>(), 0, 0);
        }

        var drops = new List<HarvestDrop>();
        foreach (var drop in definition.Drops)
        {
            var amount = _rng.NextInt(drop.Min, drop.Max + 1);
            if (amount > 0)
            {
                drops.Add(new HarvestDrop(drop.ItemId, amount));
            }
        }

        var coins = _rng.Chance(definition.CoinChance) ? 1 : 0;
        return HarvestOutcome.Landed(true, drops, definition.Xp, coins);
    }
}

using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Core.Player;

/// <summary>Player progression + inventory + wallet. All rules live here, not in Godot nodes.</summary>
public sealed class PlayerProfile
{
    public PlayerProfile(Inventory inventory, ExperienceCurve curve, int level = 1, int xp = 0, int coins = 0)
    {
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Curve = curve ?? throw new ArgumentNullException(nameof(curve));

        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be at least 1.");
        }

        if (xp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(xp), xp, "Xp cannot be negative.");
        }

        Level = Math.Min(level, curve.MaxLevel);
        Xp = xp;
        Coins = Math.Max(0, coins);
        TotalXp = curve.TotalXpForLevel(Level) + xp;
    }

    public Inventory Inventory { get; }

    public ExperienceCurve Curve { get; }

    public int Level { get; private set; }

    /// <summary>XP accumulated inside the current level.</summary>
    public int Xp { get; private set; }

    public int TotalXp { get; private set; }

    public int Coins { get; private set; }

    public int XpToNextLevel => Curve.XpToNext(Level);

    public int LevelUpCount { get; private set; }

    /// <summary>Progress towards the next level in [0, 1]. 1 at max level.</summary>
    public double LevelProgress
    {
        get
        {
            if (Level >= Curve.MaxLevel)
            {
                return 1d;
            }

            var needed = XpToNextLevel;
            return needed <= 0 ? 1d : Math.Clamp((double)Xp / needed, 0d, 1d);
        }
    }

    /// <summary>Raised once per level gained, with the new level number.</summary>
    public event Action<int>? LeveledUp;

    public event Action? Changed;

    /// <summary>Awards XP and returns the levels gained, e.g. [2, 3].</summary>
    public IReadOnlyList<int> AddXp(int amount)
    {
        if (amount <= 0)
        {
            return Array.Empty<int>();
        }

        var levels = new List<int>();
        var accepted = 0;

        while (amount > 0 && Level < Curve.MaxLevel)
        {
            var needed = XpToNextLevel - Xp;
            if (needed <= 0)
            {
                Level++;
                Xp = 0;
                levels.Add(Level);
                continue;
            }

            if (amount >= needed)
            {
                amount -= needed;
                accepted += needed;
                Xp = 0;
                Level++;
                levels.Add(Level);
            }
            else
            {
                Xp += amount;
                accepted += amount;
                amount = 0;
            }
        }

        if (accepted > 0)
        {
            TotalXp += accepted;
            Changed?.Invoke();
        }

        foreach (var level in levels)
        {
            LevelUpCount++;
            LeveledUp?.Invoke(level);
        }

        return levels;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Coins += amount;
        Changed?.Invoke();
    }

    /// <summary>
    /// Best tool in the inventory, optionally restricted to one kind (e.g. "axe"). Highest tier wins;
    /// ties go to the lower slot index so results are deterministic.
    /// </summary>
    public (string ItemId, ToolSpec Spec)? BestTool(ItemDatabase items, string? kind = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        (string ItemId, ToolSpec Spec)? best = null;

        for (var i = 0; i < Inventory.SlotCount; i++)
        {
            var slot = Inventory.GetSlot(i);
            if (slot.IsEmpty || !items.TryGet(slot.ItemId, out var definition))
            {
                continue;
            }

            if (ToolRules.Of(definition) is not { } candidate)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(kind) && !string.Equals(candidate.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (best is null || candidate.Tier > best.Value.Spec.Tier)
            {
                best = (definition.Id, candidate);
            }
        }

        return best;
    }

    /// <summary>The best tool's <see cref="ToolSpec"/>, or null when the player carries none.</summary>
    public ToolSpec? EquippedTool(ItemDatabase items) => BestTool(items)?.Spec;

    /// <summary>The best tool's item id, or null when the player carries none.</summary>
    public string? EquippedToolId(ItemDatabase items) => BestTool(items)?.ItemId;

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount)
        {
            return false;
        }

        Coins -= amount;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Applies a save file.</summary>
    public void Restore(int level, int xp, int coins, int totalXp)
    {
        Level = Math.Clamp(level, 1, Curve.MaxLevel);
        Xp = Math.Max(0, xp);
        Coins = Math.Max(0, coins);
        TotalXp = Math.Max(0, totalXp);
        Changed?.Invoke();
    }
}

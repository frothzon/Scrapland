namespace Forager.Core.Save;

/// <summary>One inventory slot in a save file.</summary>
public sealed class SavedSlot
{
    public string ItemId { get; set; } = string.Empty;

    public int Amount { get; set; }
}

/// <summary>One harvestable node instance in a save file.</summary>
public sealed class SavedNode
{
    public string NodeId { get; set; } = string.Empty;

    public int Index { get; set; }

    public int HitPointsLeft { get; set; }

    public double RespawnAtSeconds { get; set; }
}

/// <summary>
/// Versioned save payload. Keep this dumb (plain mutable data) so it can be serialised by any adapter;
/// conversion to/from live state lives in <c>SaveSerializer</c>.
/// </summary>
public sealed class SaveGame
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public int Seed { get; set; }

    public double SimSeconds { get; set; }

    public int Level { get; set; } = 1;

    public int Xp { get; set; }

    public int TotalXp { get; set; }

    public int Coins { get; set; }

    public double PlayerX { get; set; }

    public double PlayerY { get; set; }

    public double PlayerZ { get; set; }

    public List<SavedSlot> Slots { get; set; } = [];

    public List<SavedNode> Nodes { get; set; } = [];

    /// <summary>Recipe ids crafted at least once (drives <c>after:</c> unlocks). Added in v2.</summary>
    public List<string> CraftedRecipes { get; set; } = [];
}

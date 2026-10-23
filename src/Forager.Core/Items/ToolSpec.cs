namespace Forager.Core.Items;

/// <summary>
/// What a tool *is*: a kind (`axe`, `pickaxe`, `weapon`) and a tier (1 = wood, 2 = stone, 3 = iron).
/// Nodes gate on kind + minimum tier instead of an exact item id, so an iron axe still chops anything a
/// wooden one could.
/// </summary>
public readonly record struct ToolSpec(string Kind, int Tier)
{
    public static readonly ToolSpec None = new(string.Empty, 0);

    public bool IsValid => !string.IsNullOrWhiteSpace(Kind) && Tier >= 1;

    public override string ToString() => IsValid ? $"{Kind} t{Tier}" : "none";
}

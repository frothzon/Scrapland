namespace Forager.Core.Crafting;

public enum CraftStatus
{
    Success,
    UnknownRecipe,
    WrongStation,
    Locked,
    MissingInputs,
    NoSpace,
}

/// <summary>Result of one craft attempt. Like <c>HarvestOutcome</c>, it describes what happened.</summary>
public sealed class CraftOutcome
{
    private CraftOutcome(
        CraftStatus status,
        RecipeDefinition? recipe,
        IReadOnlyList<ItemAmount> consumed,
        IReadOnlyList<ItemAmount> produced,
        int xp)
    {
        Status = status;
        Recipe = recipe;
        Consumed = consumed;
        Produced = produced;
        Xp = xp;
    }

    public CraftStatus Status { get; }

    public RecipeDefinition? Recipe { get; }

    public IReadOnlyList<ItemAmount> Consumed { get; }

    public IReadOnlyList<ItemAmount> Produced { get; }

    public int Xp { get; }

    public bool Success => Status == CraftStatus.Success;

    /// <summary>Short HUD message for the outcome.</summary>
    public string Message => Status switch
    {
        CraftStatus.Success => "Crafted",
        CraftStatus.UnknownRecipe => "Unknown recipe",
        CraftStatus.WrongStation => "Wrong station",
        CraftStatus.Locked => "Locked",
        CraftStatus.MissingInputs => "Missing materials",
        CraftStatus.NoSpace => "Inventory full",
        _ => "Cannot craft",
    };

    public static CraftOutcome Fail(CraftStatus status, RecipeDefinition? recipe = null)
        => new(status, recipe, Array.Empty<ItemAmount>(), Array.Empty<ItemAmount>(), 0);

    public static CraftOutcome Crafted(
        RecipeDefinition recipe,
        IReadOnlyList<ItemAmount> consumed,
        IReadOnlyList<ItemAmount> produced,
        int xp)
        => new(CraftStatus.Success, recipe, consumed, produced, xp);
}

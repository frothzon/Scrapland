namespace Forager.Core.Crafting;

/// <summary>How a recipe becomes available.</summary>
public enum RecipeUnlockKind
{
    /// <summary>Always available.</summary>
    None,

    /// <summary>Requires the player to be at least this level.</summary>
    Level,

    /// <summary>Requires another recipe to have been crafted at least once.</summary>
    After,
}

public readonly record struct RecipeUnlock(RecipeUnlockKind Kind, int Level, string? RecipeId)
{
    public static readonly RecipeUnlock Always = new(RecipeUnlockKind.None, 1, null);

    public static RecipeUnlock AtLevel(int level) => new(RecipeUnlockKind.Level, Math.Max(1, level), null);

    public static RecipeUnlock AfterRecipe(string recipeId) => new(RecipeUnlockKind.After, 1, recipeId);

    public string ToDataString() => Kind switch
    {
        RecipeUnlockKind.Level => $"level:{Level}",
        RecipeUnlockKind.After => $"after:{RecipeId}",
        _ => "always",
    };
}

/// <summary>One item quantity inside a recipe.</summary>
public readonly record struct ItemAmount(string ItemId, int Amount);

/// <summary>A craftable recipe, loaded from <c>Data/recipes.json</c>. Crafting is instant in M2.</summary>
public sealed record RecipeDefinition(
    string Id,
    StationKind Station,
    IReadOnlyList<ItemAmount> Inputs,
    IReadOnlyList<ItemAmount> Outputs,
    int Xp,
    RecipeUnlock Unlock,
    string? UnlockError = null)
{
    public string DisplayName => string.Join(", ", Outputs.Select(output => output.ItemId));
}

namespace Forager.Core.Crafting;

/// <summary>
/// Crafting progress: which recipes have been crafted at least once. Unlocks are *derived* from this set
/// plus the player level, so there is no separate unlock list to keep in sync.
/// </summary>
public sealed class RecipeBook
{
    private readonly HashSet<string> _crafted = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Crafted => _crafted;

    public int CraftedCount => _crafted.Count;

    public void RecordCrafted(string recipeId)
    {
        if (!string.IsNullOrWhiteSpace(recipeId))
        {
            _crafted.Add(recipeId.Trim().ToLowerInvariant());
        }
    }

    public bool HasCrafted(string? recipeId) => !string.IsNullOrWhiteSpace(recipeId) && _crafted.Contains(recipeId);

    public bool IsUnlocked(RecipeDefinition recipe, int playerLevel)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return recipe.Unlock.Kind switch
        {
            RecipeUnlockKind.Level => playerLevel >= recipe.Unlock.Level,
            RecipeUnlockKind.After => HasCrafted(recipe.Unlock.RecipeId),
            _ => true,
        };
    }

    /// <summary>Human-readable reason when locked (HUD text), or null when unlocked.</summary>
    public string? LockReason(RecipeDefinition recipe, int playerLevel)
    {
        if (IsUnlocked(recipe, playerLevel))
        {
            return null;
        }

        return recipe.Unlock.Kind switch
        {
            RecipeUnlockKind.Level => $"Lv {recipe.Unlock.Level}",
            RecipeUnlockKind.After => $"Craft {recipe.Unlock.RecipeId}",
            _ => "Locked",
        };
    }

    /// <summary>Stable, sorted snapshot for save files.</summary>
    public IReadOnlyList<string> ToPersisted() => _crafted.OrderBy(id => id, StringComparer.Ordinal).ToArray();

    public void RestoreFrom(IEnumerable<string>? craftedIds)
    {
        _crafted.Clear();
        if (craftedIds is null)
        {
            return;
        }

        foreach (var id in craftedIds)
        {
            RecordCrafted(id);
        }
    }
}

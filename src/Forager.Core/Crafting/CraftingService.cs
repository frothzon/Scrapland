using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Core.Crafting;

/// <summary>
/// Crafting rules: station gate, unlock gate, inputs, output space. Crafting is instant in M2 — the only
/// time-like input is the player level used for unlocks.
/// </summary>
public sealed class CraftingService
{
    private readonly RecipeDatabase _recipes;
    private readonly ItemDatabase _items;

    public CraftingService(RecipeDatabase recipes, ItemDatabase items)
    {
        _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public RecipeDatabase Recipes => _recipes;

    /// <summary>Recipes usable at a station (hand recipes always), including locked ones.</summary>
    public IReadOnlyList<RecipeDefinition> RecipesForStation(StationKind? atStation)
        => _recipes.All
            .Where(recipe => StationAllows(recipe, atStation))
            .OrderBy(recipe => recipe.Id, StringComparer.Ordinal)
            .ToList();

    public static bool StationAllows(RecipeDefinition recipe, StationKind? atStation)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        return recipe.Station == StationKind.Hand || (atStation is { } station && station == recipe.Station);
    }

    /// <summary>Explains whether a craft would succeed, without touching the inventory.</summary>
    public CraftStatus Evaluate(
        RecipeDefinition recipe,
        Inventory inventory,
        RecipeBook book,
        int playerLevel,
        StationKind? atStation)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(book);

        if (!StationAllows(recipe, atStation))
        {
            return CraftStatus.WrongStation;
        }

        if (!book.IsUnlocked(recipe, playerLevel))
        {
            return CraftStatus.Locked;
        }

        foreach (var input in recipe.Inputs)
        {
            if (inventory.Count(input.ItemId) < input.Amount)
            {
                return CraftStatus.MissingInputs;
            }
        }

        foreach (var output in recipe.Outputs)
        {
            if (inventory.CapacityFor(output.ItemId) < output.Amount)
            {
                return CraftStatus.NoSpace;
            }
        }

        return CraftStatus.Success;
    }

    public bool CanCraft(RecipeDefinition recipe, Inventory inventory, RecipeBook book, int playerLevel, StationKind? atStation)
        => Evaluate(recipe, inventory, book, playerLevel, atStation) == CraftStatus.Success;

    public CraftOutcome Craft(
        string recipeId,
        Inventory inventory,
        RecipeBook book,
        int playerLevel,
        StationKind? atStation)
    {
        if (string.IsNullOrWhiteSpace(recipeId) || !_recipes.TryGet(recipeId, out var recipe))
        {
            return CraftOutcome.Fail(CraftStatus.UnknownRecipe);
        }

        return Craft(recipe, inventory, book, playerLevel, atStation);
    }

    /// <summary>
    /// Performs the craft atomically: either everything is consumed and produced, or nothing changes
    /// (LL‑016 class of bug). Records the craft in the book so <c>after:</c> unlocks can fire.
    /// </summary>
    public CraftOutcome Craft(
        RecipeDefinition recipe,
        Inventory inventory,
        RecipeBook book,
        int playerLevel,
        StationKind? atStation)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var status = Evaluate(recipe, inventory, book, playerLevel, atStation);
        if (status != CraftStatus.Success)
        {
            return CraftOutcome.Fail(status, recipe);
        }

        var snapshot = inventory.Capture();

        foreach (var input in recipe.Inputs)
        {
            inventory.Remove(input.ItemId, input.Amount);
        }

        foreach (var output in recipe.Outputs)
        {
            if (!inventory.TryAdd(output.ItemId, output.Amount, out _))
            {
                inventory.Restore(snapshot);
                return CraftOutcome.Fail(CraftStatus.NoSpace, recipe);
            }
        }

        book.RecordCrafted(recipe.Id);
        return CraftOutcome.Crafted(recipe, recipe.Inputs, recipe.Outputs, recipe.Xp);
    }

    /// <summary>Cheapest missing requirement for a recipe, for HUD hints (null when craftable).</summary>
    public IReadOnlyList<ItemAmount> MissingInputs(RecipeDefinition recipe, Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(inventory);

        var missing = new List<ItemAmount>();
        foreach (var input in recipe.Inputs)
        {
            var have = inventory.Count(input.ItemId);
            if (have < input.Amount)
            {
                missing.Add(new ItemAmount(input.ItemId, input.Amount - have));
            }
        }

        return missing;
    }
}

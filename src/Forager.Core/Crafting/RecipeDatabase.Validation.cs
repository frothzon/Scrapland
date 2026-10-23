using Forager.Core.Items;

namespace Forager.Core.Crafting;

public sealed partial class RecipeDatabase
{
    /// <summary>Semantic problems in the loaded data. Empty means healthy.</summary>
    public IReadOnlyList<string> Validate(ItemDatabase items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var problems = new List<string>();

        foreach (var recipe in _byId.Values)
        {
            if (recipe.UnlockError is not null)
            {
                problems.Add($"Recipe '{recipe.Id}' has a bad unlock: {recipe.UnlockError}.");
            }

            if (recipe.Xp < 0)
            {
                problems.Add($"Recipe '{recipe.Id}' has negative xp ({recipe.Xp}).");
            }

            foreach (var input in recipe.Inputs)
            {
                if (!items.Contains(input.ItemId))
                {
                    problems.Add($"Recipe '{recipe.Id}' consumes unknown item '{input.ItemId}'.");
                }

                if (input.Amount <= 0)
                {
                    problems.Add($"Recipe '{recipe.Id}' consumes '{input.ItemId}' x{input.Amount}; amounts must be positive.");
                }
            }

            foreach (var output in recipe.Outputs)
            {
                if (!items.Contains(output.ItemId))
                {
                    problems.Add($"Recipe '{recipe.Id}' produces unknown item '{output.ItemId}'.");
                }

                if (output.Amount <= 0)
                {
                    problems.Add($"Recipe '{recipe.Id}' produces '{output.ItemId}' x{output.Amount}; amounts must be positive.");
                }

                if (recipe.Inputs.Any(input => string.Equals(input.ItemId, output.ItemId, StringComparison.OrdinalIgnoreCase)))
                {
                    problems.Add($"Recipe '{recipe.Id}' both consumes and produces '{output.ItemId}'.");
                }
            }

            if (recipe.Unlock.Kind == RecipeUnlockKind.After)
            {
                if (recipe.Unlock.RecipeId is null || !_byId.ContainsKey(recipe.Unlock.RecipeId))
                {
                    problems.Add($"Recipe '{recipe.Id}' unlocks after unknown recipe '{recipe.Unlock.RecipeId}'.");
                }
                else if (string.Equals(recipe.Unlock.RecipeId, recipe.Id, StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add($"Recipe '{recipe.Id}' unlocks after itself.");
                }
            }
        }

        foreach (var cycle in new RecipeGraph(_byId).FindCycles())
        {
            problems.Add($"Recipe unlock cycle detected: {cycle}.");
        }

        return problems;
    }
}

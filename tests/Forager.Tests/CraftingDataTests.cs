using Forager.Core.Crafting;
using Forager.Core.Data;
using Forager.Core.Items;
using Forager.Core.Nodes;

namespace Forager.Tests;

/// <summary>
/// Proves the shipped crafting content is coherent: valid, acyclic, and actually reachable by playing —
/// every recipe input can be gathered or crafted, and every tool tier a node demands can be built.
/// </summary>
public sealed class CraftingDataTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);
    private static ItemDatabase Items => ItemDatabase.Load(ProjectData);
    private static NodeDatabase Nodes => NodeDatabase.Load(ProjectData);
    private static RecipeDatabase Recipes => RecipeDatabase.Load(ProjectData);

    [Fact]
    public void ShippedRecipesAndNodes_ValidateAgainstShippedItems()
    {
        var items = Items;

        var recipeProblems = Recipes.Validate(items);
        Assert.True(recipeProblems.Count == 0, "recipe problems: " + string.Join("; ", recipeProblems));

        var nodeProblems = Nodes.Validate(items);
        Assert.True(nodeProblems.Count == 0, "node problems: " + string.Join("; ", nodeProblems));
    }

    [Fact]
    public void UnlockGraph_IsAcyclic()
    {
        var graph = new RecipeGraph(Recipes.All.ToDictionary(recipe => recipe.Id, recipe => recipe, StringComparer.OrdinalIgnoreCase));

        Assert.True(graph.IsAcyclic, "cycles: " + string.Join(" | ", graph.FindCycles()));
    }

    [Fact]
    public void EveryRecipe_IsUnlockableAtMaxLevel()
    {
        var book = new RecipeBook();
        var items = Items;

        // Unlock everything by crafting in dependency order, using a generous inventory.
        var inventory = new Forager.Core.Inventories.Inventory(40, items);
        var service = new CraftingService(Recipes, items);

        var progressed = true;
        while (progressed)
        {
            progressed = false;
            foreach (var recipe in Recipes.All)
            {
                if (book.HasCrafted(recipe.Id) || !book.IsUnlocked(recipe, 99))
                {
                    continue;
                }

                foreach (var input in recipe.Inputs)
                {
                    inventory.Add(input.ItemId, input.Amount);
                }

                var outcome = service.Craft(recipe, inventory, book, 99, recipe.Station);
                if (outcome.Success)
                {
                    progressed = true;
                }
                else if (outcome.Status == CraftStatus.NoSpace)
                {
                    inventory.Clear();
                }
            }
        }

        foreach (var recipe in Recipes.All)
        {
            Assert.True(book.HasCrafted(recipe.Id), $"recipe '{recipe.Id}' could not be crafted even at max level");
        }
    }

    [Fact]
    public void EveryNodeToolRequirement_IsCraftable()
    {
        var items = Items;
        var obtainable = ObtainableItems();

        foreach (var node in Nodes.All)
        {
            if (node.RequiredTool is not { } required)
            {
                continue;
            }

            var best = obtainable
                .Where(items.Contains)
                .Select(id => ToolRules.Of(items.Get(id)))
                .Where(spec => spec is not null && spec.Value.Kind == required.Kind)
                .Select(spec => spec!.Value.Tier)
                .DefaultIfEmpty(0)
                .Max();

            Assert.True(
                best >= required.Tier,
                $"node '{node.Id}' needs {required.Kind} t{required.Tier} but the best craftable is t{best}");
        }
    }

    [Fact]
    public void EveryRecipeInput_IsObtainableFromNodesOrRecipes()
    {
        var obtainable = ObtainableItems();

        foreach (var recipe in Recipes.All)
        {
            foreach (var input in recipe.Inputs)
            {
                Assert.True(
                    obtainable.Contains(input.ItemId),
                    $"recipe '{recipe.Id}' needs '{input.ItemId}', which no node drops and no recipe produces");
            }
        }
    }

    /// <summary>Fixed-point of "node drops" ∪ "recipe outputs whose inputs are obtainable".</summary>
    private static HashSet<string> ObtainableItems()
    {
        var obtainable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in Nodes.All)
        {
            foreach (var drop in node.Drops)
            {
                obtainable.Add(drop.ItemId);
            }
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var recipe in Recipes.All)
            {
                if (recipe.Inputs.All(input => obtainable.Contains(input.ItemId)))
                {
                    foreach (var output in recipe.Outputs)
                    {
                        changed |= obtainable.Add(output.ItemId);
                    }
                }
            }
        }

        return obtainable;
    }
}

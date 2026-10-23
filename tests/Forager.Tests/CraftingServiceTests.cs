using Forager.Core.Crafting;
using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class CraftingServiceTests
{
    private const string ItemJson = """
        { "items": [
          { "id": "wood", "maxStack": 99, "icon": "icon_wood", "tags": ["resource"] },
          { "id": "plank", "maxStack": 99, "icon": "icon_plank", "tags": ["material"] },
          { "id": "rope", "maxStack": 99, "icon": "icon_rope", "tags": ["material"] },
          { "id": "charcoal", "maxStack": 99, "icon": "icon_coal", "tags": ["material"] },
          { "id": "wood_axe", "maxStack": 1, "icon": "icon_axe", "tags": ["tool", "axe"] },
          { "id": "gem", "maxStack": 99, "icon": "icon_gem", "tags": ["resource"] },
          { "id": "seed", "maxStack": 1, "icon": "icon_fiber", "tags": ["resource"] },
          { "id": "a", "maxStack": 1, "icon": "icon_stone", "tags": ["resource"] },
          { "id": "b", "maxStack": 1, "icon": "icon_stone", "tags": ["resource"] }
        ] }
        """;

    private const string RecipeJson = """
        { "recipes": [
          { "id": "plank", "station": "hand", "inputs": { "wood": 2 }, "outputs": { "plank": 1 }, "xp": 2 },
          { "id": "wood_axe", "station": "workbench", "inputs": { "plank": 2, "rope": 1 }, "outputs": { "wood_axe": 1 }, "xp": 6 },
          { "id": "charcoal", "station": "furnace", "inputs": { "wood": 3 }, "outputs": { "charcoal": 1 }, "xp": 3 },
          { "id": "level_locked", "station": "hand", "inputs": { "wood": 1 }, "outputs": { "rope": 1 }, "unlock": "level:5" },
          { "id": "chained", "station": "hand", "inputs": { "wood": 1 }, "outputs": { "gem": 1 }, "unlock": "after:plank" },
          { "id": "split", "station": "hand", "inputs": { "seed": 1 }, "outputs": { "a": 1, "b": 1 } }
        ] }
        """;

    private static readonly ItemDatabase Items = ItemDatabase.FromJson(ItemJson);
    private static readonly RecipeDatabase Recipes = RecipeDatabase.FromJson(RecipeJson);

    private static (CraftingService Service, Inventory Inventory, RecipeBook Book) NewContext(int slots = 8)
        => (new CraftingService(Recipes, Items), new Inventory(slots, Items), new RecipeBook());

    [Fact]
    public void Craft_ConsumesProducesAwardsXpAndRecordsTheRecipe()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 5);

        var outcome = service.Craft("plank", inventory, book, playerLevel: 1, atStation: null);

        Assert.True(outcome.Success);
        Assert.Equal(3, inventory.Count("wood"));
        Assert.Equal(1, inventory.Count("plank"));
        Assert.Equal(2, outcome.Xp);
        Assert.True(book.HasCrafted("plank"));
    }

    [Fact]
    public void HandRecipes_WorkAtAnyStationAndAtNone()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 10);

        Assert.True(service.Craft("plank", inventory, book, 1, null).Success);
        Assert.True(service.Craft("plank", inventory, book, 1, StationKind.Workbench).Success);
        Assert.True(service.Craft("plank", inventory, book, 1, StationKind.Furnace).Success);
    }

    [Fact]
    public void StationRecipes_RequireTheirOwnStation()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("plank", 6);
        inventory.Add("rope", 3);

        Assert.Equal(CraftStatus.WrongStation, service.Craft("wood_axe", inventory, book, 1, null).Status);
        Assert.Equal(CraftStatus.WrongStation, service.Craft("wood_axe", inventory, book, 1, StationKind.Furnace).Status);

        var atWorkbench = service.Craft("wood_axe", inventory, book, 1, StationKind.Workbench);
        Assert.True(atWorkbench.Success);
        Assert.Equal(1, inventory.Count("wood_axe"));
        Assert.Equal(4, inventory.Count("plank"));
        Assert.Equal(2, inventory.Count("rope"));
    }

    [Fact]
    public void MissingInputs_ChangeNothing()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 1);

        var outcome = service.Craft("plank", inventory, book, 1, null);

        Assert.Equal(CraftStatus.MissingInputs, outcome.Status);
        Assert.Equal(1, inventory.Count("wood"));
        Assert.Equal(0, inventory.Count("plank"));
        Assert.False(book.HasCrafted("plank"));

        var missing = service.MissingInputs(Recipes.Get("plank"), inventory);
        Assert.Equal(new ItemAmount("wood", 1), Assert.Single(missing));
    }

    [Fact]
    public void LevelLockedRecipes_RefuseUntilTheLevelIsReached()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 5);

        Assert.Equal(CraftStatus.Locked, service.Craft("level_locked", inventory, book, 4, null).Status);
        Assert.Equal(0, inventory.Count("rope"));

        Assert.True(service.Craft("level_locked", inventory, book, 5, null).Success);
        Assert.Equal(1, inventory.Count("rope"));
    }

    [Fact]
    public void AfterRecipes_UnlockOnceTheDependencyWasCrafted()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 10);

        Assert.Equal(CraftStatus.Locked, service.Craft("chained", inventory, book, 99, null).Status);

        service.Craft("plank", inventory, book, 1, null);

        Assert.True(service.Craft("chained", inventory, book, 1, null).Success);
        Assert.Equal(1, inventory.Count("gem"));
    }

    [Fact]
    public void NoSpace_RollsBackEverything()
    {
        var (service, inventory, book) = NewContext(slots: 2);
        inventory.Add("seed", 1);
        inventory.Add("a", 1);

        var outcome = service.Craft("split", inventory, book, 1, null);

        Assert.Equal(CraftStatus.NoSpace, outcome.Status);
        Assert.Equal(1, inventory.Count("seed"));
        Assert.Equal(1, inventory.Count("a"));
        Assert.Equal(0, inventory.Count("b"));
        Assert.False(book.HasCrafted("split"));
    }

    [Fact]
    public void UnknownRecipe_IsReported()
    {
        var (service, inventory, book) = NewContext();

        Assert.Equal(CraftStatus.UnknownRecipe, service.Craft("ghost", inventory, book, 1, null).Status);
        Assert.Equal(CraftStatus.UnknownRecipe, service.Craft("  ", inventory, book, 1, null).Status);
    }

    [Fact]
    public void RecipesForStation_IncludesHandRecipesAndTheStationOnly()
    {
        var service = new CraftingService(Recipes, Items);

        var atWorkbench = service.RecipesForStation(StationKind.Workbench).Select(recipe => recipe.Id).ToArray();

        Assert.Contains("plank", atWorkbench);
        Assert.Contains("wood_axe", atWorkbench);
        Assert.DoesNotContain("charcoal", atWorkbench);

        var handOnly = service.RecipesForStation(null).Select(recipe => recipe.Id).ToArray();
        Assert.Contains("plank", handOnly);
        Assert.DoesNotContain("wood_axe", handOnly);
        Assert.DoesNotContain("charcoal", handOnly);
    }

    [Fact]
    public void CraftedItems_CanBeTurnedIntoStrongerTools()
    {
        var (service, inventory, book) = NewContext();
        inventory.Add("wood", 10);
        inventory.Add("rope", 5);

        service.Craft("plank", inventory, book, 1, null);
        service.Craft("plank", inventory, book, 1, null);
        var axe = service.Craft("wood_axe", inventory, book, 1, StationKind.Workbench);

        Assert.True(axe.Success);
        Assert.Equal(1, inventory.Count("wood_axe"));
        Assert.Equal(0, inventory.Count("plank"));
    }
}


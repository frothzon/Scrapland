using Forager.Core.Crafting;

namespace Forager.Tests;

public sealed class RecipeBookTests
{
    private static RecipeDefinition Recipe(string id, RecipeUnlock unlock)
        => new(id, StationKind.Hand, [new ItemAmount("wood", 1)], [new ItemAmount("plank", 1)], 1, unlock);

    [Fact]
    public void AlwaysRecipes_AreUnlockedAtLevelOne()
    {
        var book = new RecipeBook();
        var recipe = Recipe("plank", RecipeUnlock.Always);

        Assert.True(book.IsUnlocked(recipe, 1));
        Assert.Null(book.LockReason(recipe, 1));
    }

    [Fact]
    public void LevelRecipes_UnlockAtTheRightLevel()
    {
        var book = new RecipeBook();
        var recipe = Recipe("stone_axe", RecipeUnlock.AtLevel(3));

        Assert.False(book.IsUnlocked(recipe, 2));
        Assert.Equal("Lv 3", book.LockReason(recipe, 2));
        Assert.True(book.IsUnlocked(recipe, 3));
        Assert.True(book.IsUnlocked(recipe, 10));
    }

    [Fact]
    public void AfterRecipes_UnlockOnceTheOtherWasCrafted()
    {
        var book = new RecipeBook();
        var recipe = Recipe("iron_axe", RecipeUnlock.AfterRecipe("iron_ingot"));

        Assert.False(book.IsUnlocked(recipe, 99));
        Assert.Equal("Craft iron_ingot", book.LockReason(recipe, 99));

        book.RecordCrafted("iron_ingot");

        Assert.True(book.IsUnlocked(recipe, 1));
        Assert.Null(book.LockReason(recipe, 1));
    }

    [Fact]
    public void RecordCrafted_IsCaseInsensitive_AndIgnoresBlankIds()
    {
        var book = new RecipeBook();

        book.RecordCrafted("IRON_INGOT");
        book.RecordCrafted("  ");
        book.RecordCrafted(null!);

        Assert.True(book.HasCrafted("iron_ingot"));
        Assert.Equal(1, book.CraftedCount);
    }

    [Fact]
    public void PersistedSnapshot_IsSortedAndRestorable()
    {
        var book = new RecipeBook();
        book.RecordCrafted("rope");
        book.RecordCrafted("plank");

        var persisted = book.ToPersisted();
        Assert.Equal(new[] { "plank", "rope" }, persisted);

        var restored = new RecipeBook();
        restored.RestoreFrom(persisted);
        Assert.True(restored.HasCrafted("plank"));
        Assert.True(restored.HasCrafted("rope"));

        restored.RestoreFrom(null);
        Assert.Equal(0, restored.CraftedCount);
    }
}

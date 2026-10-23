using Forager.Core.Crafting;
using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class RecipeDatabaseTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);
    private static ItemDatabase Items => ItemDatabase.Load(ProjectData);
    private static RecipeDatabase Recipes => RecipeDatabase.Load(ProjectData);

    [Fact]
    public void ShippedRecipes_LoadAndValidate()
    {
        var recipes = Recipes;

        Assert.True(recipes.Count >= 12, $"expected a starter recipe set, found {recipes.Count}");
        var problems = recipes.Validate(Items);
        Assert.True(problems.Count == 0, "recipe problems: " + string.Join("; ", problems));
    }

    [Fact]
    public void ShippedRecipes_HaveEveryStationRepresented()
    {
        var recipes = Recipes;

        Assert.NotEmpty(recipes.ForStation(StationKind.Hand));
        Assert.NotEmpty(recipes.ForStation(StationKind.Workbench));
        Assert.NotEmpty(recipes.ForStation(StationKind.Furnace));
        Assert.Empty(recipes.ForStation(StationKind.Hand).Where(recipe => recipe.Station != StationKind.Hand));
    }

    [Fact]
    public void FromJson_RejectsDuplicateIdsAndUnknownStations()
    {
        const string duplicate = """
            { "recipes": [ { "id": "a", "station": "hand", "inputs": {"wood":1}, "outputs": {"plank":1} },
                           { "id": "A", "station": "hand", "inputs": {"wood":1}, "outputs": {"plank":1} } ] }
            """;
        Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson(duplicate));

        const string badStation = """
            { "recipes": [ { "id": "a", "station": "moon", "inputs": {"wood":1}, "outputs": {"plank":1} } ] }
            """;
        var exception = Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson(badStation));
        Assert.Contains("station", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_RejectsEmptySidesAndBrokenJson()
    {
        Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson(
            """{ "recipes": [ { "id": "a", "station": "hand", "outputs": {"plank":1} } ] }"""));
        Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson(
            """{ "recipes": [ { "id": "a", "station": "hand", "inputs": {"wood":1} } ] }"""));
        Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson("{ nope"));
        Assert.Throws<InvalidDataException>(() => RecipeDatabase.FromJson("{}"));
    }

    [Fact]
    public void Validate_FlagsUnknownItemsAndSelfConsumingRecipes()
    {
        var recipes = RecipeDatabase.FromJson("""
            { "recipes": [
              { "id": "ghost", "station": "hand", "inputs": {"unobtainium":1}, "outputs": {"plank":1} },
              { "id": "loop", "station": "hand", "inputs": {"plank":1}, "outputs": {"plank":1} },
              { "id": "negative", "station": "hand", "inputs": {"wood":1}, "outputs": {"plank":1}, "xp": -5 }
            ] }
            """);

        var problems = recipes.Validate(Items);

        Assert.Contains(problems, p => p.Contains("unknown item 'unobtainium'", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("both consumes and produces", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("negative xp", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FlagsBadAndUnresolvedUnlocks()
    {
        var recipes = RecipeDatabase.FromJson("""
            { "recipes": [
              { "id": "typo", "station": "hand", "inputs": {"wood":1}, "outputs": {"plank":1}, "unlock": "skil:industry" },
              { "id": "orphan", "station": "hand", "inputs": {"wood":1}, "outputs": {"rope":1}, "unlock": "after:does_not_exist" },
              { "id": "selfish", "station": "hand", "inputs": {"wood":1}, "outputs": {"charcoal":1}, "unlock": "after:selfish" }
            ] }
            """);

        var problems = recipes.Validate(Items);

        Assert.Contains(problems, p => p.Contains("not a known unlock", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("unlocks after unknown recipe", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("unlocks after itself", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DetectsUnlockCycles()
    {
        var recipes = RecipeDatabase.FromJson("""
            { "recipes": [
              { "id": "a", "station": "hand", "inputs": {"wood":1}, "outputs": {"plank":1}, "unlock": "after:b" },
              { "id": "b", "station": "hand", "inputs": {"wood":1}, "outputs": {"rope":1}, "unlock": "after:a" }
            ] }
            """);

        var problems = recipes.Validate(Items);

        Assert.Contains(problems, p => p.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Load_ThrowsHelpfully_WhenMissing()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => RecipeDatabase.Load(new InMemoryDataSource()));
        Assert.Contains("Data/recipes.json", exception.Message);
    }
}

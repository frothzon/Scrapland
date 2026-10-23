using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class ItemDatabaseTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);

    [Fact]
    public void ShippedItemsFile_LoadsAndIsValid()
    {
        var database = ItemDatabase.Load(ProjectData);

        Assert.True(database.Count >= 10, $"expected a starter item set, found {database.Count}");
        Assert.True(database.Contains("wood"));
        Assert.True(database.Contains("coin"));

        var problems = database.Validate();
        Assert.True(problems.Count == 0, "item data problems: " + string.Join("; ", problems));
    }

    [Fact]
    public void ShippedItemsFile_DeclaresRequiredStarterResources()
    {
        var database = ItemDatabase.Load(ProjectData);
        string[] expected = ["wood", "stone", "fiber", "berry", "coin", "iron_ore"];

        foreach (var id in expected)
        {
            Assert.True(database.TryGet(id, out var definition), $"missing starter item '{id}'");
            Assert.True(definition.MaxStack > 0, $"'{id}' must have a positive stack size");
            Assert.False(string.IsNullOrWhiteSpace(definition.Icon), $"'{id}' must reference an icon");
        }
    }

    [Fact]
    public void MaxStack_UsesData_AndFallsBackToOneForUnknownIds()
    {
        var database = ItemDatabase.FromJson("""
            { "items": [ { "id": "wood", "maxStack": 50 } ] }
            """);

        Assert.Equal(50, database.MaxStack("wood"));
        Assert.Equal(1, database.MaxStack("does_not_exist"));
    }

    [Fact]
    public void FromJson_DefaultsNameToId_AndTagsToEmpty()
    {
        var database = ItemDatabase.FromJson("""
            { "items": [ { "id": "fiber" } ] }
            """);

        var definition = database.Get("fiber");
        Assert.Equal("fiber", definition.Name);
        Assert.Empty(definition.Tags);
        Assert.Equal(99, definition.MaxStack);
        Assert.False(definition.HasTag("plant"));
    }

    [Fact]
    public void FromJson_RejectsDuplicateIds()
    {
        const string json = """
            { "items": [ { "id": "wood" }, { "id": "WOOD" } ] }
            """;

        var exception = Assert.Throws<InvalidDataException>(() => ItemDatabase.FromJson(json));
        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_RejectsMissingIds_AndBrokenJson()
    {
        Assert.Throws<InvalidDataException>(() => ItemDatabase.FromJson("""{ "items": [ { "name": "No id" } ] }"""));
        Assert.Throws<InvalidDataException>(() => ItemDatabase.FromJson("{ not json"));
        Assert.Throws<InvalidDataException>(() => ItemDatabase.FromJson("{}"));
    }

    [Fact]
    public void Load_ThrowsHelpfully_WhenFileIsMissing()
    {
        var source = new InMemoryDataSource();

        var exception = Assert.Throws<FileNotFoundException>(() => ItemDatabase.Load(source));
        Assert.Contains("Data/items.json", exception.Message);
    }

    [Fact]
    public void Tags_AreCaseInsensitive_AndIconsResolveToNames()
    {
        var database = ItemDatabase.Load(ProjectData);
        var wood = database.Get("wood");

        Assert.True(wood.HasTag("RESOURCE"));
        Assert.False(string.IsNullOrWhiteSpace(wood.Icon));
    }
}

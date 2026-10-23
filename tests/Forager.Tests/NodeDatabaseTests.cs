using Forager.Core.Data;
using Forager.Core.Items;
using Forager.Core.Nodes;

namespace Forager.Tests;

public sealed class NodeDatabaseTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);
    private static ItemDatabase Items => ItemDatabase.Load(ProjectData);
    private static NodeDatabase Nodes => NodeDatabase.Load(ProjectData);

    [Fact]
    public void ShippedNodeFile_LoadsAndIsValid()
    {
        var nodes = Nodes;

        Assert.True(nodes.Count >= 3, $"expected at least 3 node types, found {nodes.Count}");
        Assert.True(nodes.Contains("birch"));
        Assert.True(nodes.Contains("rock"));
        Assert.True(nodes.Contains("bush"));

        var problems = nodes.Validate(Items);
        Assert.True(problems.Count == 0, "node problems: " + string.Join("; ", problems));
    }

    [Fact]
    public void ShippedNodes_ReferenceRealAssetsAndItems()
    {
        var nodes = Nodes;

        foreach (var node in nodes.All)
        {
            Assert.StartsWith("res://Assets/", node.MeshPath);
            Assert.EndsWith(".obj", node.MeshPath);
            Assert.True(node.HitPoints > 0);
            Assert.True(node.Drops.Count > 0);
            Assert.All(node.Drops, drop => Assert.True(Items.Contains(drop.ItemId)));
        }
    }

    [Fact]
    public void BushIsHandGatherable_AndTreesNeedAnAxe()
    {
        var nodes = Nodes;

        Assert.False(nodes.Get("bush").RequiresTool);
        Assert.Equal("wood_axe", nodes.Get("birch").RequiredTool);
        Assert.Equal("pickaxe", nodes.Get("rock").RequiredTool);
    }

    [Fact]
    public void FromJson_RejectsDuplicateIds()
    {
        const string json = """
            { "nodes": [ { "id": "rock" }, { "id": "ROCK" } ] }
            """;

        var exception = Assert.Throws<InvalidDataException>(() => NodeDatabase.FromJson(json));
        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_DefaultsMissingFields()
    {
        var nodes = NodeDatabase.FromJson("""{ "nodes": [ { "id": "twig", "drops": [{ "item": "wood" }] } ] }""");
        var twig = nodes.Get("twig");

        Assert.Equal("twig", twig.DisplayName);
        Assert.Equal("prop", twig.Kind);
        Assert.Equal(1, twig.HitPoints);
        Assert.Equal(1, twig.Drops[0].Min);
    }

    [Fact]
    public void Validate_FlagsUnknownToolDropAndBadPaths()
    {
        var nodes = NodeDatabase.FromJson("""
            { "nodes": [
              { "id": "bad_tool", "meshPath": "C:/nope.obj", "hitPoints": 0, "respawnSeconds": -1, "xp": -2,
                "coinChance": 2, "requiredTool": "laser", "drops": [ { "item": "unobtainium", "min": 0, "max": 0 } ] }
            ] }
            """);

        var problems = nodes.Validate(Items);

        Assert.Contains(problems, p => p.Contains("meshPath", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("hit point", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("respawn", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("coinChance", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("unknown tool", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("unknown item", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_ThrowsHelpfully_WhenMissing()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => NodeDatabase.Load(new InMemoryDataSource()));
        Assert.Contains("Data/nodes.json", exception.Message);
    }
}

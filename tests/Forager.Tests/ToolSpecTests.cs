using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class ToolSpecTests
{
    private static ItemDatabase Items => ItemDatabase.Load(new FileSystemDataSource(TestPaths.GodotProjectRoot));

    private static ItemDefinition Item(string id, params string[] tags)
        => new(id, id, 1, 0, tags, "icon", null, 1);

    [Fact]
    public void ShippedTools_ExposeKindsAndTiers()
    {
        var items = Items;

        var woodAxe = ToolRules.Of(items.Get("wood_axe"))!.Value;
        Assert.Equal("axe", woodAxe.Kind);
        Assert.Equal(1, woodAxe.Tier);

        Assert.Equal(new ToolSpec("axe", 2), ToolRules.Of(items.Get("stone_axe")));
        Assert.Equal(new ToolSpec("axe", 3), ToolRules.Of(items.Get("iron_axe")));
        Assert.Equal(new ToolSpec("pickaxe", 3), ToolRules.Of(items.Get("iron_pickaxe")));
        Assert.Equal("weapon", ToolRules.Of(items.Get("iron_sword"))!.Value.Kind);
    }

    [Fact]
    public void KindFallsBackToTheFirstNonToolTag()
    {
        var spec = ToolRules.Of(Item("mystery_axe", "tool", "axe"));

        Assert.Equal(new ToolSpec("axe", 1), spec);
    }

    [Fact]
    public void ExplicitKindWins_OverTags()
    {
        var definition = new ItemDefinition("weird", "Weird", 1, 0, ["tool", "axe"], "icon", "pickaxe", 2);

        Assert.Equal(new ToolSpec("pickaxe", 2), ToolRules.Of(definition));
    }

    [Fact]
    public void NonTools_HaveNoSpec()
    {
        Assert.Null(ToolRules.Of(Item("wood", "resource")));
        Assert.Null(ToolRules.Of(Item("tool_only", "tool")));
    }

    [Theory]
    [InlineData("axe", 1, "axe", 1, true)]   // same tier
    [InlineData("axe", 3, "axe", 1, true)]   // better tool, easier node
    [InlineData("axe", 1, "axe", 2, false)]  // wooden axe vs stone node
    [InlineData("pickaxe", 3, "axe", 1, false)] // wrong kind
    [InlineData("AXE", 2, "axe", 1, true)]   // case-insensitive kind
    public void Satisfies_Matrix(string equippedKind, int equippedTier, string requiredKind, int requiredTier, bool expected)
    {
        var equipped = new ToolSpec(equippedKind, equippedTier);
        var required = new ToolSpec(requiredKind, requiredTier);

        Assert.Equal(expected, ToolRules.Satisfies(required, equipped));
    }

    [Fact]
    public void Satisfies_IsTrueWhenNoToolIsRequired_AndFalseWithoutATool()
    {
        Assert.True(ToolRules.Satisfies(null, null));
        Assert.True(ToolRules.Satisfies(null, new ToolSpec("axe", 1)));
        Assert.False(ToolRules.Satisfies(new ToolSpec("axe", 1), null));
        Assert.False(ToolRules.Satisfies(new ToolSpec("axe", 1), ToolSpec.None));
    }
}

using Forager.Core.Data;
using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Tests;

/// <summary>
/// Guards the layering rule from agents.md: Forager.Core must stay engine-free so the whole
/// Tier-1 suite can run without Godot. If someone adds a Godot reference, this fails.
/// </summary>
public sealed class ArchitectureTests
{
    [Fact]
    public void CoreAssembly_DoesNotReferenceGodot()
    {
        var assembly = typeof(Inventory).Assembly;

        var godotReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            godotReferences.Length == 0,
            "Forager.Core must not reference Godot; found: " + string.Join(", ", godotReferences));
    }

    [Fact]
    public void CoreAssembly_OnlyDependsOnFrameworkAssemblies()
    {
        var assembly = typeof(ItemDatabase).Assembly;

        var nonFramework = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                           && !name.StartsWith("netstandard", StringComparison.Ordinal)
                           && !name.StartsWith("Microsoft.", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            nonFramework.Length == 0,
            "Forager.Core gained new dependencies: " + string.Join(", ", nonFramework));
    }

    [Fact]
    public void DataSourceAbstraction_IsUsableWithoutDisk()
    {
        IDataSource source = new InMemoryDataSource()
            .Add("Data/items.json", """{ "items": [ { "id": "wood", "maxStack": 42 } ] }""");

        var database = ItemDatabase.Load(source);

        Assert.Equal(42, database.MaxStack("wood"));
        Assert.True(source.Exists("Data/items.json"));
        Assert.False(source.Exists("Data/missing.json"));
    }
}

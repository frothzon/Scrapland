using Forager.Core.Data;
using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class InventoryCapacityTests
{
    private static ItemDatabase Items => ItemDatabase.Load(new FileSystemDataSource(TestPaths.GodotProjectRoot));

    private static Inventory NewInventory(int slots = 2) => new(slots, new FixedStackLimits(10));

    [Fact]
    public void CapacityFor_CountsEmptySlotsAndPartialStacks()
    {
        var inventory = NewInventory(2);

        Assert.Equal(20, inventory.CapacityFor("wood"));

        inventory.Add("wood", 6);
        Assert.Equal(14, inventory.CapacityFor("wood"));

        inventory.Add("stone", 4);
        Assert.Equal(4, inventory.CapacityFor("wood"));
        Assert.Equal(6, inventory.CapacityFor("stone"));
    }

    [Fact]
    public void CanAdd_RespectsStacksAndFreeSlots()
    {
        var inventory = NewInventory(2);
        inventory.Add("wood", 15);

        Assert.True(inventory.CanAdd("wood", 5));
        Assert.False(inventory.CanAdd("wood", 6));
        Assert.False(inventory.CanAdd("wood", 0));
        Assert.False(inventory.CanAdd("wood", -1));
    }

    [Fact]
    public void TryAdd_IsAllOrNothing()
    {
        var inventory = NewInventory(1);
        inventory.Add("wood", 8);

        Assert.False(inventory.TryAdd("wood", 5, out var added));
        Assert.Equal(0, added);
        Assert.Equal(8, inventory.Count("wood"));

        Assert.True(inventory.TryAdd("wood", 2, out added));
        Assert.Equal(2, added);
        Assert.Equal(10, inventory.Count("wood"));
    }

    [Fact]
    public void TryAdd_SpillsAcrossSlots()
    {
        var inventory = NewInventory(2);

        Assert.True(inventory.TryAdd("wood", 15, out var added));
        Assert.Equal(15, added);
        Assert.Equal(15, inventory.Count("wood"));
    }

    [Fact]
    public void Snapshot_Restore_RollsBackExactly()
    {
        var inventory = NewInventory(3);
        inventory.Add("wood", 7);
        inventory.Add("stone", 2);
        var before = inventory.Capture();

        inventory.Remove("wood", 5);
        inventory.Add("berry", 3);
        inventory.Restore(before);

        Assert.Equal(7, inventory.Count("wood"));
        Assert.Equal(2, inventory.Count("stone"));
        Assert.Equal(0, inventory.Count("berry"));
    }

    [Fact]
    public void Snapshot_FromADifferentSlotCount_IsRejected()
    {
        var snapshot = NewInventory(2).Capture();

        Assert.Throws<ArgumentException>(() => NewInventory(3).Restore(snapshot));
    }
}

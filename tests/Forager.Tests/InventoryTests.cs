using Forager.Core.Inventories;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class InventoryTests
{
    private static Inventory NewInventory(int slots = 4, int maxStack = 10)
        => new(slots, new FixedStackLimits(maxStack));

    [Fact]
    public void NewInventory_IsEmpty()
    {
        var inventory = NewInventory();

        Assert.Equal(4, inventory.SlotCount);
        Assert.Equal(0, inventory.Count("wood"));
        Assert.True(inventory.GetSlot(0).IsEmpty);
        Assert.True(inventory.IsFull("wood") is false);
    }

    [Fact]
    public void Add_StacksIntoOneSlot_WhenItFits()
    {
        var inventory = NewInventory();

        var leftover = inventory.Add("wood", 7);

        Assert.Equal(0, leftover);
        Assert.Equal(new ItemStack("wood", 7), inventory.GetSlot(0));
    }

    [Fact]
    public void Add_SpillsIntoNextSlot_WhenStackOverflows()
    {
        var inventory = NewInventory();

        var leftover = inventory.Add("wood", 15);

        Assert.Equal(0, leftover);
        Assert.Equal(new ItemStack("wood", 10), inventory.GetSlot(0));
        Assert.Equal(new ItemStack("wood", 5), inventory.GetSlot(1));
        Assert.Equal(15, inventory.Count("wood"));
    }

    [Fact]
    public void Add_FillsPartialStacksBeforeUsingEmptySlots()
    {
        var inventory = NewInventory(4, 10);
        inventory.Add("wood", 6);
        inventory.Add("stone", 2);

        inventory.Add("wood", 6);

        Assert.Equal(new ItemStack("wood", 10), inventory.GetSlot(0));
        Assert.Equal(new ItemStack("stone", 2), inventory.GetSlot(1));
        Assert.Equal(new ItemStack("wood", 2), inventory.GetSlot(2));
    }

    [Fact]
    public void Add_ReturnsLeftover_WhenInventoryIsFull()
    {
        var inventory = NewInventory(2, 10);

        var leftover = inventory.Add("wood", 25);

        Assert.Equal(5, leftover);
        Assert.Equal(20, inventory.Count("wood"));
        Assert.True(inventory.IsFull("wood"));
        // No free slot and nothing stackable: another item cannot fit either.
        Assert.True(inventory.IsFull("stone"));
    }

    [Fact]
    public void IsFull_IsFalse_WhenAnEmptySlotRemains()
    {
        var inventory = NewInventory(3, 10);
        inventory.Add("wood", 20);

        Assert.False(inventory.IsFull("stone"));
    }

    [Fact]
    public void Remove_TakesFromLowestSlotIndexFirst()
    {
        var inventory = NewInventory(3, 10);
        inventory.Add("wood", 15);

        var removed = inventory.Remove("wood", 12);

        Assert.Equal(12, removed);
        Assert.Equal(0, inventory.GetSlot(0).Amount);
        Assert.True(inventory.GetSlot(0).IsEmpty);
        Assert.Equal(3, inventory.GetSlot(1).Amount);
        Assert.Equal(3, inventory.Count("wood"));
    }

    [Fact]
    public void Remove_ReportsActualAmount_WhenInsufficient()
    {
        var inventory = NewInventory();
        inventory.Add("wood", 3);

        var removed = inventory.Remove("wood", 5);

        Assert.Equal(3, removed);
        Assert.Equal(0, inventory.Count("wood"));
    }

    [Fact]
    public void TryRemove_IsAtomic_WhenInsufficient()
    {
        var inventory = NewInventory();
        inventory.Add("wood", 3);

        var success = inventory.TryRemove("wood", 5, out var removed);

        Assert.False(success);
        Assert.Equal(0, removed);
        Assert.Equal(3, inventory.Count("wood"));
    }

    [Fact]
    public void Count_IsCaseInsensitive()
    {
        var inventory = NewInventory();
        inventory.Add("Wood", 4);

        Assert.Equal(4, inventory.Count("wood"));
        Assert.True(inventory.Has("WOOD", 4));
    }

    [Fact]
    public void Changed_FiresOncePerSuccessfulMutation()
    {
        var inventory = NewInventory();
        var changes = 0;
        inventory.Changed += () => changes++;

        inventory.Add("wood", 1);
        inventory.Remove("wood", 1);
        inventory.Remove("wood", 1);

        Assert.Equal(2, changes);
    }

    [Fact]
    public void Add_RejectsNonPositiveAmounts()
    {
        var inventory = NewInventory();

        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add("wood", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add("wood", -1));
        Assert.Throws<ArgumentException>(() => inventory.Add("  ", 1));
    }
}

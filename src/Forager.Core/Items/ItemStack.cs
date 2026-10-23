namespace Forager.Core.Items;

/// <summary>A quantity of one item type inside a single inventory slot.</summary>
public readonly record struct ItemStack(string ItemId, int Amount)
{
    public static readonly ItemStack Empty = new(string.Empty, 0);

    public bool IsEmpty => Amount <= 0 || string.IsNullOrEmpty(ItemId);

    public override string ToString() => IsEmpty ? "<empty>" : $"{ItemId} x{Amount}";
}

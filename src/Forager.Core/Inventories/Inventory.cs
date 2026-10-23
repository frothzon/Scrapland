using Forager.Core.Items;

namespace Forager.Core.Inventories;

/// <summary>
/// Fixed-size, slot-based inventory (Forager style). Pure data + rules: no engine types,
/// no wall clock, fully unit-testable. Engine nodes observe it through <see cref="Changed"/>.
/// </summary>
public sealed class Inventory
{
    private readonly string[] _itemIds;
    private readonly int[] _amounts;
    private readonly IStackLimits _limits;

    public Inventory(int slotCount, IStackLimits limits)
    {
        if (slotCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "Inventory needs at least one slot.");
        }

        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        _itemIds = new string[slotCount];
        _amounts = new int[slotCount];
        Array.Fill(_itemIds, string.Empty);
    }

    public int SlotCount => _itemIds.Length;

    /// <summary>Raised after any successful mutation.</summary>
    public event Action? Changed;

    public ItemStack GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Slot index out of range.");
        }

        return _amounts[index] <= 0 ? ItemStack.Empty : new ItemStack(_itemIds[index], _amounts[index]);
    }

    public bool IsFull(string itemId)
    {
        for (var i = 0; i < SlotCount; i++)
        {
            if (_amounts[i] <= 0 || (IsSameItem(i, itemId) && _amounts[i] < _limits.MaxStack(itemId)))
            {
                return false;
            }
        }

        return true;
    }

    public int Count(string itemId)
    {
        var total = 0;
        for (var i = 0; i < SlotCount; i++)
        {
            if (_amounts[i] > 0 && IsSameItem(i, itemId))
            {
                total += _amounts[i];
            }
        }

        return total;
    }

    public bool Has(string itemId, int amount) => amount > 0 && Count(itemId) >= amount;

    /// <summary>How many more of <paramref name="itemId"/> would fit right now (partial stacks + free slots).</summary>
    public int CapacityFor(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        var maxStack = _limits.MaxStack(itemId);
        var capacity = 0;

        for (var i = 0; i < SlotCount; i++)
        {
            if (_amounts[i] <= 0)
            {
                capacity += maxStack;
            }
            else if (IsSameItem(i, itemId) && _amounts[i] < maxStack)
            {
                capacity += maxStack - _amounts[i];
            }
        }

        return capacity;
    }

    /// <summary>True when <paramref name="amount"/> of the item fits without dropping anything.</summary>
    public bool CanAdd(string itemId, int amount) => amount > 0 && CapacityFor(itemId) >= amount;

    /// <summary>All-or-nothing add. Returns false and changes nothing when the items would not fit.</summary>
    public bool TryAdd(string itemId, int amount, out int added)
    {
        added = 0;
        if (!CanAdd(itemId, amount))
        {
            return false;
        }

        var leftover = Add(itemId, amount);
        added = amount - leftover;
        return leftover == 0;
    }

    /// <summary>Adds items, filling partial stacks first. Returns the amount that did not fit.</summary>
    public int Add(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
        }

        var maxStack = _limits.MaxStack(itemId);
        var remaining = amount;

        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_amounts[i] <= 0 || !IsSameItem(i, itemId) || _amounts[i] >= maxStack)
            {
                continue;
            }

            var moved = Math.Min(maxStack - _amounts[i], remaining);
            _amounts[i] += moved;
            remaining -= moved;
        }

        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_amounts[i] > 0)
            {
                continue;
            }

            var moved = Math.Min(maxStack, remaining);
            _itemIds[i] = itemId;
            _amounts[i] = moved;
            remaining -= moved;
        }

        if (remaining != amount)
        {
            Changed?.Invoke();
        }

        return remaining;
    }

    /// <summary>Removes items from the lowest slot index upward. Returns how many were actually removed.</summary>
    public int Remove(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
        }

        var remaining = amount;
        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_amounts[i] <= 0 || !IsSameItem(i, itemId))
            {
                continue;
            }

            var taken = Math.Min(_amounts[i], remaining);
            _amounts[i] -= taken;
            remaining -= taken;
            if (_amounts[i] <= 0)
            {
                _amounts[i] = 0;
                _itemIds[i] = string.Empty;
            }
        }

        var removed = amount - remaining;
        if (removed > 0)
        {
            Changed?.Invoke();
        }

        return removed;
    }

    public bool TryRemove(string itemId, int amount, out int removed)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || Count(itemId) < amount)
        {
            removed = 0;
            return false;
        }

        removed = Remove(itemId, amount);
        return true;
    }

    public void Clear()
    {
        Array.Fill(_itemIds, string.Empty);
        Array.Clear(_amounts, 0, _amounts.Length);
        Changed?.Invoke();
    }

    /// <summary>
    /// Copy of the slot contents. Used to make multi-step operations (crafting) all-or-nothing:
    /// capture, mutate, and <see cref="Restore"/> if a later step fails.
    /// </summary>
    public Snapshot Capture() => new((string[])_itemIds.Clone(), (int[])_amounts.Clone());

    /// <summary>Restores a snapshot taken by <see cref="Capture"/>.</summary>
    public void Restore(Snapshot snapshot)
    {
        if (snapshot.ItemIds.Length != SlotCount || snapshot.Amounts.Length != SlotCount)
        {
            throw new ArgumentException("Snapshot does not match this inventory's slot count.", nameof(snapshot));
        }

        Array.Copy(snapshot.ItemIds, _itemIds, SlotCount);
        Array.Copy(snapshot.Amounts, _amounts, SlotCount);
        Changed?.Invoke();
    }

    /// <summary>Opaque slot snapshot (arrays are copies, safe to keep across mutations).</summary>
    public readonly record struct Snapshot(string[] ItemIds, int[] Amounts);

    private bool IsSameItem(int index, string itemId)
        => string.Equals(_itemIds[index], itemId, StringComparison.OrdinalIgnoreCase);
}

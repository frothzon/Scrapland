namespace Forager.Core.Items;

/// <summary>Resolves how many units of an item fit in one inventory slot.</summary>
public interface IStackLimits
{
    int MaxStack(string itemId);
}

/// <summary>Uniform stack limit, used by tests and by the bootstrap before data is loaded.</summary>
public sealed class FixedStackLimits : IStackLimits
{
    private readonly int _maxStack;

    public FixedStackLimits(int maxStack = 99)
    {
        if (maxStack <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStack), maxStack, "Stack limit must be positive.");
        }

        _maxStack = maxStack;
    }

    public int MaxStack(string itemId) => _maxStack;
}

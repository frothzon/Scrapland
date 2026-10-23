namespace Forager.Core.Randomness;

/// <summary>Randomness abstraction so every random draw in gameplay is seedable and testable.</summary>
public interface IRng
{
    double NextDouble();

    int NextInt(int minInclusive, int maxExclusive);

    bool Chance(double probability);

    T Pick<T>(IReadOnlyList<T> items);
}

/// <summary>
/// Deterministic RNG. Uses a stable (FNV-1a) label hash for forking so stream layout never
/// depends on .NET's randomized string hashing (see lessons_learned.md LL-009).
/// </summary>
public sealed class SeededRng : IRng
{
    private readonly System.Random _random;

    public SeededRng(int seed)
    {
        Seed = seed;
        _random = new System.Random(seed);
    }

    public int Seed { get; }

    /// <summary>Creates an independent stream derived from this one's seed and a label.</summary>
    public static SeededRng Fork(int seed, string label)
        => new(unchecked((int)((uint)seed * 16777619u ^ StableHash(label))));

    public static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var character in text)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        return hash;
    }

    public double NextDouble() => _random.NextDouble();

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "maxExclusive must be greater than minInclusive.");
        }

        return _random.Next(minInclusive, maxExclusive);
    }

    public bool Chance(double probability)
    {
        if (probability <= 0)
        {
            return false;
        }

        return probability >= 1 || _random.NextDouble() < probability;
    }

    public T Pick<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
        }

        return items[_random.Next(items.Count)];
    }
}

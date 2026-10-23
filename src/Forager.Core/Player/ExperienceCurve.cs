namespace Forager.Core.Player;

/// <summary>
/// XP curve: <c>xpToNext(level) = round(baseXp * level^exponent)</c>. Parameters come from
/// <c>Data/balance.json</c> so balancing never needs a code change.
/// </summary>
public sealed class ExperienceCurve
{
    public ExperienceCurve(int baseXp = 20, double exponent = 1.5, int maxLevel = 30)
    {
        if (baseXp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseXp), baseXp, "baseXp must be positive.");
        }

        if (exponent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "exponent must be positive.");
        }

        if (maxLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLevel), maxLevel, "maxLevel must be at least 1.");
        }

        BaseXp = baseXp;
        Exponent = exponent;
        MaxLevel = maxLevel;
    }

    public int BaseXp { get; }

    public double Exponent { get; }

    public int MaxLevel { get; }

    /// <summary>XP needed to go from <paramref name="level"/> to the next one. 0 at max level.</summary>
    public int XpToNext(int level)
    {
        if (level >= MaxLevel)
        {
            return 0;
        }

        var clamped = Math.Max(1, level);
        return (int)Math.Round(BaseXp * Math.Pow(clamped, Exponent), MidpointRounding.AwayFromZero);
    }

    /// <summary>Total XP required to have reached <paramref name="level"/> from level 1.</summary>
    public int TotalXpForLevel(int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        var total = 0;
        for (var current = 1; current < Math.Min(level, MaxLevel); current++)
        {
            total += XpToNext(current);
        }

        return total;
    }

    /// <summary>Level reached after accumulating <paramref name="totalXp"/>.</summary>
    public int LevelForTotalXp(int totalXp)
    {
        var remaining = Math.Max(0, totalXp);
        var level = 1;
        while (level < MaxLevel)
        {
            var needed = XpToNext(level);
            if (remaining < needed)
            {
                break;
            }

            remaining -= needed;
            level++;
        }

        return level;
    }
}

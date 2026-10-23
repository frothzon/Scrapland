using Forager.Core.Player;

namespace Forager.Tests;

public sealed class ExperienceCurveTests
{
    [Fact]
    public void XpToNext_IsPositiveAndMonotonic()
    {
        var curve = new ExperienceCurve(baseXp: 20, exponent: 1.5, maxLevel: 10);
        var previous = 0;

        for (var level = 1; level < curve.MaxLevel; level++)
        {
            var needed = curve.XpToNext(level);
            Assert.True(needed > previous, $"level {level} should need more xp than level {level - 1}");
            previous = needed;
        }

        Assert.Equal(0, curve.XpToNext(curve.MaxLevel));
    }

    [Fact]
    public void TotalXpForLevel_MatchesSumOfSteps()
    {
        var curve = new ExperienceCurve(20, 1.5, 30);

        Assert.Equal(0, curve.TotalXpForLevel(1));
        Assert.Equal(curve.XpToNext(1), curve.TotalXpForLevel(2));
        Assert.Equal(curve.XpToNext(1) + curve.XpToNext(2), curve.TotalXpForLevel(3));
    }

    [Fact]
    public void LevelForTotalXp_RoundTripsEveryLevel()
    {
        var curve = new ExperienceCurve(20, 1.5, 20);

        for (var level = 1; level <= curve.MaxLevel; level++)
        {
            var total = curve.TotalXpForLevel(level);
            Assert.Equal(level, curve.LevelForTotalXp(total));
        }

        Assert.Equal(1, curve.LevelForTotalXp(0));
        Assert.Equal(curve.MaxLevel, curve.LevelForTotalXp(int.MaxValue));
    }

    [Fact]
    public void Constructor_ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceCurve(baseXp: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceCurve(exponent: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceCurve(maxLevel: 0));
    }
}

using Forager.Core.Sim;

namespace Forager.Tests;

public sealed class SimClockTests
{
    [Fact]
    public void Advance_TracksTimeOfDayWithinOneDay()
    {
        var clock = new SimClock(secondsPerDay: 100);

        clock.Advance(25);

        Assert.Equal(0.25, clock.TimeOfDay, 6);
        Assert.Equal(0, clock.Day);
    }

    [Fact]
    public void Advance_RollsOverIntoNextDay()
    {
        var clock = new SimClock(secondsPerDay: 100);

        clock.Advance(150);

        Assert.Equal(1, clock.Day);
        Assert.Equal(0.5, clock.TimeOfDay, 6);
        Assert.Equal(150, clock.TotalSeconds, 6);
    }

    [Fact]
    public void DayStarted_FiresOncePerCrossedDay()
    {
        var clock = new SimClock(secondsPerDay: 100);
        var days = new List<long>();
        clock.DayStarted += days.Add;

        clock.Advance(350);

        Assert.Equal(new long[] { 1, 2, 3 }, days);
        Assert.Equal(3, clock.Day);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.24, true)]
    [InlineData(0.30, false)]
    [InlineData(0.80, false)]
    [InlineData(0.90, true)]
    public void IsNight_MatchesDaylightWindow(double fractionOfDay, bool expectedNight)
    {
        var clock = new SimClock(secondsPerDay: 100);

        clock.Advance(fractionOfDay * 100);

        Assert.Equal(expectedNight, clock.IsNight);
    }

    [Fact]
    public void AdvanceTicks_IsExactForFixedStepSimulation()
    {
        var clock = new SimClock(secondsPerDay: 1000);

        clock.AdvanceTicks(ticks: 600, tickSeconds: 1d / 60d);

        Assert.Equal(10, clock.TotalSeconds, 9);
    }

    [Fact]
    public void Advance_RejectsNegativeOrNonFiniteDeltas()
    {
        var clock = new SimClock();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(double.PositiveInfinity));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveDayLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimClock(0));
    }
}

namespace Forager.Core.Sim;

/// <summary>
/// Engine-independent simulation clock. All time-based systems (respawns, crops, bank interest,
/// day/night) advance through this instead of <c>DateTime</c> or engine timers so headless tests
/// can fast-forward deterministically.
/// </summary>
public sealed class SimClock
{
    /// <summary>Real seconds per in-game day. 480 s = 8 minutes, matching Forager's pace.</summary>
    public const double DefaultSecondsPerDay = 480d;

    /// <summary>Daylight window, as a fraction of the day. Matches the IsNight property.</summary>
    public const double NightStart = 0.8333333333333334;
    public const double NightEnd = 0.25;

    public SimClock(double secondsPerDay = DefaultSecondsPerDay)
    {
        if (secondsPerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(secondsPerDay), secondsPerDay, "A day must last a positive number of seconds.");
        }

        SecondsPerDay = secondsPerDay;
    }

    public double SecondsPerDay { get; }

    public long Day { get; private set; }

    public double TotalSeconds { get; private set; }

    /// <summary>Position within the current day, in [0, 1).</summary>
    public double TimeOfDay => TotalSeconds / SecondsPerDay % 1d;

    public bool IsNight => TimeOfDay < NightEnd || TimeOfDay >= NightStart;

    /// <summary>Raised once per in-game day, with the new day index.</summary>
    public event Action<long>? DayStarted;

    public void Advance(double deltaSeconds)
    {
        if (deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time cannot run backwards.");
        }

        if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Delta must be finite.");
        }

        TotalSeconds += deltaSeconds;

        var dayIndex = (long)Math.Floor(TotalSeconds / SecondsPerDay);
        while (Day < dayIndex)
        {
            Day++;
            DayStarted?.Invoke(Day);
        }
    }

    /// <summary>Advances by whole fixed ticks, which is what the headless harness uses.</summary>
    public void AdvanceTicks(int ticks, double tickSeconds)
    {
        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), ticks, "Tick count cannot be negative.");
        }

        Advance(ticks * tickSeconds);
    }

    /// <summary>
    /// Jumps the clock to an absolute time (save/load). Does not raise <see cref="DayStarted"/>: loading a
    /// game must not replay every day boundary that happened while it was closed.
    /// </summary>
    public void RestoreTo(double totalSeconds)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSeconds), totalSeconds, "Restored time must be finite and non-negative.");
        }

        TotalSeconds = totalSeconds;
        Day = (long)Math.Floor(totalSeconds / SecondsPerDay);
    }
}

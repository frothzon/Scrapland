using Forager.Core.Data;
using Forager.Core.Inventories;
using Forager.Core.Items;
using Forager.Core.Player;
using Forager.Core.Save;
using Forager.Core.Sim;

namespace Forager.Tests;

public sealed class SaveSerializerTests
{
    private static ItemDatabase Items => ItemDatabase.Load(new FileSystemDataSource(TestPaths.GodotProjectRoot));

    private static PlayerProfile NewProfile() => new(new Inventory(24, Items), new ExperienceCurve(20, 1.5, 30));

    [Fact]
    public void Capture_ThenRestore_RestoresInventoryXpCoinsAndTime()
    {
        var profile = NewProfile();
        var clock = new SimClock();
        profile.Inventory.Add("wood", 7);
        profile.Inventory.Add("stone", 3);
        profile.Inventory.Add("berry", 12);
        profile.AddXp(45);
        profile.AddCoins(9);
        clock.Advance(123.5);

        var save = SaveSerializer.Capture(profile, clock, seed: 4242);

        var loadedProfile = NewProfile();
        var loadedClock = new SimClock();
        SaveSerializer.Restore(SaveSerializer.FromJson(SaveSerializer.ToJson(save)), loadedProfile, loadedClock);

        Assert.Equal(4242, save.Seed);
        Assert.Equal(123.5, loadedClock.TotalSeconds, 6);
        Assert.Equal(profile.Level, loadedProfile.Level);
        Assert.Equal(profile.Xp, loadedProfile.Xp);
        Assert.Equal(profile.Coins, loadedProfile.Coins);
        Assert.Equal(7, loadedProfile.Inventory.Count("wood"));
        Assert.Equal(3, loadedProfile.Inventory.Count("stone"));
        Assert.Equal(12, loadedProfile.Inventory.Count("berry"));
    }

    [Fact]
    public void Restore_ClearsTheInventoryFirst()
    {
        var profile = NewProfile();
        profile.Inventory.Add("wood", 5);
        var empty = SaveSerializer.Capture(NewProfile(), new SimClock(), 1);

        SaveSerializer.Restore(empty, profile, new SimClock());

        Assert.Equal(0, profile.Inventory.Count("wood"));
    }

    [Fact]
    public void Json_IsVersionedAndReadable()
    {
        var json = SaveSerializer.ToJson(SaveSerializer.Capture(NewProfile(), new SimClock(), 1337));

        Assert.Contains("\"version\": 1", json);
        Assert.Contains("\"seed\": 1337", json);
    }

    [Fact]
    public void FromJson_RejectsNewerVersions()
    {
        const string json = """{ "version": 99, "seed": 1 }""";

        var exception = Assert.Throws<InvalidDataException>(() => SaveSerializer.FromJson(json));
        Assert.Contains("99", exception.Message);
    }

    [Fact]
    public void FromJson_RejectsInvalidVersionAndBrokenJson()
    {
        Assert.Throws<InvalidDataException>(() => SaveSerializer.FromJson("""{ "version": 0 }"""));
        Assert.Throws<InvalidDataException>(() => SaveSerializer.FromJson("{ nope"));
    }

    [Fact]
    public void FromJson_ToleratesMissingArraysAndClampsValues()
    {
        var save = SaveSerializer.FromJson("""{ "version": 1, "level": 0, "xp": -5, "coins": -1 }""");

        Assert.Empty(save.Slots);
        Assert.Empty(save.Nodes);
        Assert.Equal(1, save.Level);
        Assert.Equal(0, save.Xp);
        Assert.Equal(0, save.Coins);
    }

    [Fact]
    public void InMemoryPersistence_RoundTrips()
    {
        var persistence = new InMemoryPersistence();
        Assert.False(persistence.TryLoad(out _));
        Assert.False(persistence.Exists);

        persistence.Save("{\"version\":1}");

        Assert.True(persistence.Exists);
        Assert.True(persistence.TryLoad(out var json));
        Assert.Equal("{\"version\":1}", json);

        persistence.Delete();
        Assert.False(persistence.Exists);
    }

    [Fact]
    public void SimClock_RestoreTo_JumpsWithoutReplayingDays()
    {
        var clock = new SimClock(secondsPerDay: 100);
        var days = new List<long>();
        clock.DayStarted += days.Add;

        clock.RestoreTo(350);

        Assert.Equal(350, clock.TotalSeconds, 6);
        Assert.Equal(3, clock.Day);
        Assert.Empty(days);

        clock.Advance(50);
        Assert.Equal(new long[] { 4 }, days);
    }
}

using Forager.Core.Data;
using Forager.Core.Inventories;
using Forager.Core.Items;
using Forager.Core.Player;

namespace Forager.Tests;

public sealed class PlayerProfileTests
{
    private static ItemDatabase Items => ItemDatabase.Load(new FileSystemDataSource(TestPaths.GodotProjectRoot));

    private static PlayerProfile NewProfile(int slots = 24)
    {
        var items = Items;
        return new PlayerProfile(new Inventory(slots, items), new ExperienceCurve(20, 1.5, 30));
    }

    [Fact]
    public void NewProfile_StartsAtLevelOneWithNothing()
    {
        var profile = NewProfile();

        Assert.Equal(1, profile.Level);
        Assert.Equal(0, profile.Xp);
        Assert.Equal(0, profile.Coins);
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(0, profile.LevelProgress);
        Assert.Equal(20, profile.XpToNextLevel);
    }

    [Fact]
    public void AddXp_FillsTheBarWithoutLevelling()
    {
        var profile = NewProfile();

        var levels = profile.AddXp(5);

        Assert.Empty(levels);
        Assert.Equal(1, profile.Level);
        Assert.Equal(5, profile.Xp);
        Assert.Equal(0.25, profile.LevelProgress, 6);
        Assert.Equal(5, profile.TotalXp);
    }

    [Fact]
    public void AddXp_LevelsUpAndRaisesEvent()
    {
        var profile = NewProfile();
        var levelsSeen = new List<int>();
        profile.LeveledUp += levelsSeen.Add;

        var levels = profile.AddXp(20);

        Assert.Equal(new[] { 2 }, levels);
        Assert.Equal(new[] { 2 }, levelsSeen);
        Assert.Equal(2, profile.Level);
        Assert.Equal(0, profile.Xp);
        Assert.Equal(20, profile.TotalXp);
        Assert.Equal(1, profile.LevelUpCount);
    }

    [Fact]
    public void AddXp_CanGainMultipleLevelsAtOnce()
    {
        var profile = NewProfile();

        var levels = profile.AddXp(500);

        Assert.True(levels.Count >= 2, $"expected several levels, got {levels.Count}");
        Assert.Equal(levels.Count, profile.LevelUpCount);
        Assert.Equal(profile.Level, levels[^1]);
        Assert.True(profile.Level > 2);
    }

    [Fact]
    public void AddXp_StopsAtMaxLevel()
    {
        var profile = new PlayerProfile(new Inventory(4, Items), new ExperienceCurve(10, 1.0, 3));

        profile.AddXp(10_000);

        Assert.Equal(3, profile.Level);
        Assert.Equal(0, profile.Xp);
        Assert.Equal(1, profile.LevelProgress);
        Assert.Empty(profile.AddXp(50));
    }

    [Fact]
    public void Coins_AddAndSpend()
    {
        var profile = NewProfile();
        var changes = 0;
        profile.Changed += () => changes++;

        profile.AddCoins(15);

        Assert.Equal(15, profile.Coins);
        Assert.True(profile.TrySpendCoins(10));
        Assert.Equal(5, profile.Coins);
        Assert.False(profile.TrySpendCoins(10));
        Assert.False(profile.TrySpendCoins(0));
        Assert.Equal(5, profile.Coins);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Inventory_IsSharedWithProfile()
    {
        var profile = NewProfile();

        var leftover = profile.Inventory.Add("wood", 5);

        Assert.Equal(0, leftover);
        Assert.Equal(5, profile.Inventory.Count("wood"));
    }

    [Fact]
    public void Restore_AppliesSavedValues()
    {
        var profile = NewProfile();

        profile.Restore(level: 4, xp: 7, coins: 33, totalXp: 123);

        Assert.Equal(4, profile.Level);
        Assert.Equal(7, profile.Xp);
        Assert.Equal(33, profile.Coins);
        Assert.Equal(123, profile.TotalXp);
    }
}

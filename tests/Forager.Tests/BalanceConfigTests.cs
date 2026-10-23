using Forager.Core.Config;
using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Tests;

public sealed class BalanceConfigTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);
    private static ItemDatabase Items => ItemDatabase.Load(ProjectData);

    [Fact]
    public void ShippedBalanceFile_LoadsAndIsValid()
    {
        var balance = BalanceConfig.Load(ProjectData);

        var problems = balance.Validate(Items);
        Assert.True(problems.Count == 0, "balance problems: " + string.Join("; ", problems));
    }

    [Fact]
    public void ShippedBalanceFile_HasExpectedShape()
    {
        var balance = BalanceConfig.Load(ProjectData);

        Assert.True(balance.Camera.PitchDegrees < 0, "camera must look down at the ground");
        Assert.True(balance.Camera.Distance > 0);
        Assert.True(balance.Player.RunSpeed > balance.Player.WalkSpeed);
        Assert.True(balance.Gather.SwingSeconds > 0);
        Assert.True(balance.World.NodeCount > 0);
        Assert.Contains("wood_axe", balance.StartingItems);
    }

    [Fact]
    public void MissingSections_FallBackToDefaults()
    {
        var balance = BalanceConfig.FromJson("{}");

        Assert.Equal(-55, balance.Camera.PitchDegrees);
        Assert.Equal(3.5, balance.Player.WalkSpeed);
        Assert.Empty(balance.StartingItems);
    }

    [Fact]
    public void Validate_FlagsBadNumbersAndUnknownItems()
    {
        var balance = BalanceConfig.FromJson("""
            {
              "player": { "walkSpeed": 0, "runSpeed": 1, "acceleration": -1, "gravity": 0, "modelScale": -2 },
              "camera": { "pitchDegrees": 20, "distance": 0, "followSharpness": 0 },
              "gather": { "rangeMeters": 0, "swingSeconds": 0, "magnetRadius": -1, "magnetSpeed": 0 },
              "progression": { "baseXp": 0, "curveExponent": 0, "maxLevel": 0 },
              "world": { "plotSize": 0, "nodeCount": -5 },
              "startingItems": ["not_an_item"]
            }
            """);

        var problems = balance.Validate(Items);

        Assert.Contains(problems, p => p.Contains("walkSpeed", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("acceleration", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("pitchDegrees", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("startingItems references unknown item", StringComparison.Ordinal));
        Assert.True(problems.Count >= 10, $"expected many problems, got {problems.Count}");
    }

    [Fact]
    public void Validate_FlagsRunSpeedSlowerThanWalkSpeed()
    {
        var balance = BalanceConfig.FromJson("""{ "player": { "walkSpeed": 6, "runSpeed": 3 } }""");

        var problems = balance.Validate(Items);

        Assert.Contains(problems, p => p.Contains("runSpeed", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_ThrowsHelpfully_WhenMissing()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => BalanceConfig.Load(new InMemoryDataSource()));
        Assert.Contains("Data/balance.json", exception.Message);
    }

    [Fact]
    public void FromJson_RejectsBrokenJson()
    {
        Assert.Throws<InvalidDataException>(() => BalanceConfig.FromJson("{ oops"));
    }
}

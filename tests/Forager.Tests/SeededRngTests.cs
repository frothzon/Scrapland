using Forager.Core.Randomness;

namespace Forager.Tests;

public sealed class SeededRngTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalSequences()
    {
        var first = new SeededRng(1337);
        var second = new SeededRng(1337);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(first.NextInt(0, 1000), second.NextInt(0, 1000));
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var first = new SeededRng(1);
        var second = new SeededRng(2);

        var firstSequence = Enumerable.Range(0, 50).Select(_ => first.NextInt(0, 1_000_000)).ToArray();
        var secondSequence = Enumerable.Range(0, 50).Select(_ => second.NextInt(0, 1_000_000)).ToArray();

        Assert.NotEqual(firstSequence, secondSequence);
    }

    [Fact]
    public void Fork_IsDeterministic_AndLabelDependent()
    {
        var firstA = SeededRng.Fork(42, "worldgen");
        var secondA = SeededRng.Fork(42, "worldgen");
        var otherB = SeededRng.Fork(42, "loot");

        var a = Enumerable.Range(0, 32).Select(_ => firstA.NextInt(0, 1_000_000)).ToArray();
        var a2 = Enumerable.Range(0, 32).Select(_ => secondA.NextInt(0, 1_000_000)).ToArray();
        var b = Enumerable.Range(0, 32).Select(_ => otherB.NextInt(0, 1_000_000)).ToArray();

        Assert.Equal(a, a2);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void StableHash_IsIndependentOfProcessRandomization()
    {
        // Hardcoded FNV-1a expectations: if these ever break, worldgen seeds change between runs.
        Assert.Equal(0x811C9DC5u, SeededRng.StableHash(string.Empty));
        Assert.Equal(0xB505B30Fu, SeededRng.StableHash("worldgen"));
    }

    [Fact]
    public void NextInt_StaysWithinRange_AndRejectsEmptyRange()
    {
        var rng = new SeededRng(7);

        for (var i = 0; i < 500; i++)
        {
            var value = rng.NextInt(-5, 5);
            Assert.InRange(value, -5, 4);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));
    }

    [Fact]
    public void Chance_HandlesBoundaries()
    {
        var rng = new SeededRng(3);

        Assert.False(rng.Chance(0));
        Assert.True(rng.Chance(1));
        Assert.False(rng.Chance(-1));
    }

    [Fact]
    public void Pick_ReturnsMemberOfList_AndRejectsEmpty()
    {
        var rng = new SeededRng(11);
        string[] options = ["a", "b", "c"];

        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(rng.Pick(options), options);
        }

        Assert.Throws<ArgumentException>(() => rng.Pick(Array.Empty<string>()));
    }
}

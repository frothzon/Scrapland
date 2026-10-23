using Forager.Core.Nodes;
using Forager.Core.World;

namespace Forager.Tests;

public sealed class WorldPlacementTests
{
    private static NodeDefinition Def(string id) => new(
        id,
        "tree",
        id,
        $"res://Assets/{id}.obj",
        1,
        null,
        1,
        10,
        1,
        0,
        [new DropSpec("wood", 1, 1)]);

    private static readonly NodeDefinition[] Definitions = [Def("a"), Def("b"), Def("c")];

    [Fact]
    public void SameSeed_ProducesIdenticalLayout()
    {
        var first = PlotPlacement.Generate(Definitions, 14, 16, seed: 1337);
        var second = PlotPlacement.Generate(Definitions, 14, 16, seed: 1337);

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentLayout()
    {
        var first = PlotPlacement.Generate(Definitions, 14, 16, seed: 1);
        var second = PlotPlacement.Generate(Definitions, 14, 16, seed: 2);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Placement_StaysInsideThePlot()
    {
        var placed = PlotPlacement.Generate(Definitions, 14, 16, seed: 99);
        var half = 8d;

        Assert.Equal(14, placed.Count);
        Assert.All(placed, node =>
        {
            Assert.InRange(node.X, -half, half);
            Assert.InRange(node.Z, -half, half);
        });
    }

    [Fact]
    public void Placement_RespectsGridSpacing()
    {
        var placed = PlotPlacement.Generate(Definitions, 14, 16, seed: 5);
        var columns = (int)Math.Ceiling(Math.Sqrt(14));
        var cell = 16d / columns;
        var minimum = cell * 0.5;

        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                var dx = placed[i].X - placed[j].X;
                var dz = placed[i].Z - placed[j].Z;
                var distance = Math.Sqrt((dx * dx) + (dz * dz));
                Assert.True(distance >= minimum, $"nodes {i} and {j} are only {distance:0.00} m apart");
            }
        }
    }

    [Fact]
    public void Placement_OnlyUsesKnownDefinitions()
    {
        var placed = PlotPlacement.Generate(Definitions, 20, 16, seed: 7);
        var ids = Definitions.Select(definition => definition.Id).ToArray();

        Assert.All(placed, node => Assert.Contains(node.DefinitionId, ids));
        Assert.True(placed.Select(node => node.DefinitionId).Distinct().Count() > 1, "expected a mix of node types");
    }

    [Fact]
    public void ZeroCount_ReturnsEmpty()
    {
        Assert.Empty(PlotPlacement.Generate(Definitions, 0, 16, 1));
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(() => PlotPlacement.Generate(Array.Empty<NodeDefinition>(), 4, 16, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlotPlacement.Generate(Definitions, 4, 0, 1));
    }
}

using Forager.Core.Nodes;
using Forager.Core.Randomness;

namespace Forager.Core.World;

/// <summary>A node instance placed on the plot.</summary>
public sealed record PlacedNode(string DefinitionId, double X, double Z);

/// <summary>
/// Deterministic plot layout. Nodes are placed on a jittered grid, so spacing is guaranteed and the same
/// seed always produces the same world (tested by <c>WorldPlacementTests</c>).
/// </summary>
public static class PlotPlacement
{
    public static IReadOnlyList<PlacedNode> Generate(
        IReadOnlyList<NodeDefinition> definitions,
        int count,
        double plotSize,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (definitions.Count == 0)
        {
            throw new ArgumentException("At least one node definition is required.", nameof(definitions));
        }

        if (plotSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plotSize), plotSize, "Plot size must be positive.");
        }

        if (count <= 0)
        {
            return Array.Empty<PlacedNode>();
        }

        var rng = new SeededRng(seed);
        var columns = (int)Math.Ceiling(Math.Sqrt(count));
        var cell = plotSize / columns;
        var half = plotSize / 2d;
        var jitter = cell * 0.25;

        var placed = new List<PlacedNode>(count);
        for (var index = 0; index < count; index++)
        {
            var column = index % columns;
            var row = index / columns;

            var x = (-half) + ((column + 0.5) * cell) + ((rng.NextDouble() * 2d) - 1d) * jitter;
            var z = (-half) + ((row + 0.5) * cell) + ((rng.NextDouble() * 2d) - 1d) * jitter;
            var definition = definitions[rng.NextInt(0, definitions.Count)];

            placed.Add(new PlacedNode(definition.Id, Math.Round(x, 3), Math.Round(z, 3)));
        }

        return placed;
    }
}

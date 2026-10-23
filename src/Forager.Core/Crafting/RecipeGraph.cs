namespace Forager.Core.Crafting;

/// <summary>
/// Unlock dependency graph between recipes (<c>after:&lt;recipeId&gt;</c>). Used to prove the data has no
/// cycles, which would otherwise make recipes permanently locked.
/// </summary>
public sealed class RecipeGraph
{
    private readonly Dictionary<string, string> _edges;

    public RecipeGraph(IReadOnlyDictionary<string, RecipeDefinition> recipes)
    {
        ArgumentNullException.ThrowIfNull(recipes);

        _edges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in recipes.Values)
        {
            if (recipe.Unlock.Kind == RecipeUnlockKind.After && !string.IsNullOrWhiteSpace(recipe.Unlock.RecipeId))
            {
                _edges[recipe.Id] = recipe.Unlock.RecipeId!;
            }
        }
    }

    public IReadOnlyDictionary<string, string> Edges => _edges;

    /// <summary>Returns "a -> b -> a" style descriptions for every cycle found (empty when acyclic).</summary>
    public IReadOnlyList<string> FindCycles()
    {
        var cycles = new List<string>();
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0 unvisited, 1 visiting, 2 done
        var path = new List<string>();

        foreach (var start in _edges.Keys)
        {
            Walk(start, state, path, cycles);
        }

        return cycles;
    }

    public bool IsAcyclic => FindCycles().Count == 0;

    private void Walk(string node, Dictionary<string, int> state, List<string> path, List<string> cycles)
    {
        state.TryGetValue(node, out var status);
        if (status == 2)
        {
            return;
        }

        if (status == 1)
        {
            var start = path.FindIndex(entry => string.Equals(entry, node, StringComparison.OrdinalIgnoreCase));
            var cycle = start >= 0 ? path.Skip(start).ToList() : [.. path];
            cycle.Add(node);
            cycles.Add(string.Join(" -> ", cycle));
            return;
        }

        state[node] = 1;
        path.Add(node);

        if (_edges.TryGetValue(node, out var next))
        {
            Walk(next, state, path, cycles);
        }

        path.RemoveAt(path.Count - 1);
        state[node] = 2;
    }
}

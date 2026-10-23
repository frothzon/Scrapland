namespace Forager.Core.Nodes;

/// <summary>One guaranteed drop range for a node.</summary>
public sealed record DropSpec(string ItemId, int Min, int Max);

/// <summary>
/// Definition of a harvestable world node (tree, rock, bush, …). Definitions come from
/// <c>Data/nodes.json</c>; gameplay code must never hardcode node ids or numbers.
/// </summary>
public sealed record NodeDefinition(
    string Id,
    string Kind,
    string DisplayName,
    string MeshPath,
    double Scale,
    string? RequiredTool,
    int HitPoints,
    double RespawnSeconds,
    int Xp,
    double CoinChance,
    IReadOnlyList<DropSpec> Drops)
{
    public bool RequiresTool => !string.IsNullOrWhiteSpace(RequiredTool);
}

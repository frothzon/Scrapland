using System.Text.Json;
using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Core.Nodes;

/// <summary>All harvestable node definitions, loaded from <c>Data/nodes.json</c>.</summary>
public sealed class NodeDatabase
{
    public const string DefaultPath = "Data/nodes.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, NodeDefinition> _byId;

    private NodeDatabase(Dictionary<string, NodeDefinition> byId)
    {
        _byId = byId;
    }

    public IReadOnlyCollection<NodeDefinition> All => _byId.Values;

    public int Count => _byId.Count;

    public static NodeDatabase Load(IDataSource source, string path = DefaultPath)
    {
        if (!source.Exists(path))
        {
            throw new FileNotFoundException($"Node data file '{path}' was not found.", path);
        }

        return FromJson(source.ReadText(path), path);
    }

    public static NodeDatabase FromJson(string json, string origin = "<memory>")
    {
        NodeFileDto? file;
        try
        {
            file = JsonSerializer.Deserialize<NodeFileDto>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Node data '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (file?.Nodes is null)
        {
            throw new InvalidDataException($"Node data '{origin}' has no 'nodes' array.");
        }

        var byId = new Dictionary<string, NodeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in file.Nodes)
        {
            var id = dto.Id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidDataException($"Node data '{origin}' contains a node without an id.");
            }

            if (byId.ContainsKey(id))
            {
                throw new InvalidDataException($"Node data '{origin}' contains duplicate node id '{id}'.");
            }

            var drops = dto.Drops is null
                ? Array.Empty<DropSpec>()
                : dto.Drops
                    .Where(drop => !string.IsNullOrWhiteSpace(drop.Item))
                    .Select(drop => new DropSpec(drop.Item!, Math.Max(1, drop.Min), Math.Max(drop.Min, drop.Max)))
                    .ToArray();

            byId[id] = new NodeDefinition(
                id,
                string.IsNullOrWhiteSpace(dto.Kind) ? "prop" : dto.Kind!,
                string.IsNullOrWhiteSpace(dto.DisplayName) ? id : dto.DisplayName!,
                dto.MeshPath?.Trim() ?? string.Empty,
                dto.Scale <= 0 ? 1d : dto.Scale,
                string.IsNullOrWhiteSpace(dto.RequiredTool) ? null : dto.RequiredTool!.Trim().ToLowerInvariant(),
                dto.ToolTier,
                dto.HitPoints,
                dto.RespawnSeconds,
                dto.Xp,
                dto.CoinChance,
                drops);
        }

        return new NodeDatabase(byId);
    }

    public bool TryGet(string nodeId, out NodeDefinition definition) => _byId.TryGetValue(nodeId, out definition!);

    public NodeDefinition Get(string nodeId)
        => _byId.TryGetValue(nodeId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown node id '{nodeId}'.");

    public bool Contains(string nodeId) => _byId.ContainsKey(nodeId);

    /// <summary>Semantic problems in the loaded data. Empty means healthy.</summary>
    public IReadOnlyList<string> Validate(ItemDatabase items)
    {
        var problems = new List<string>();

        foreach (var node in _byId.Values)
        {
            if (!node.MeshPath.StartsWith("res://", StringComparison.Ordinal))
            {
                problems.Add($"Node '{node.Id}' has an invalid meshPath '{node.MeshPath}'.");
            }

            if (node.Scale <= 0)
            {
                problems.Add($"Node '{node.Id}' has a non-positive scale ({node.Scale}).");
            }

            if (node.HitPoints <= 0)
            {
                problems.Add($"Node '{node.Id}' must have at least 1 hit point.");
            }

            if (node.RespawnSeconds < 0)
            {
                problems.Add($"Node '{node.Id}' has a negative respawn time ({node.RespawnSeconds}).");
            }

            if (node.Xp < 0)
            {
                problems.Add($"Node '{node.Id}' has negative xp ({node.Xp}).");
            }

            if (node.CoinChance is < 0 or > 1)
            {
                problems.Add($"Node '{node.Id}' coinChance must be within 0..1 ({node.CoinChance}).");
            }

            if (node.RequiredToolTier < 1)
            {
                problems.Add($"Node '{node.Id}' has toolTier {node.RequiredToolTier}; tiers start at 1.");
            }

            if (node.RequiredTool is { } required && !HasToolAtTier(items, required))
            {
                problems.Add($"Node '{node.Id}' requires tool '{required.Kind}' at tier {required.Tier}, which no item provides.");
            }

            if (node.Drops.Count == 0)
            {
                problems.Add($"Node '{node.Id}' drops nothing.");
            }

            foreach (var drop in node.Drops)
            {
                if (!items.Contains(drop.ItemId))
                {
                    problems.Add($"Node '{node.Id}' drops unknown item '{drop.ItemId}'.");
                }

                if (drop.Min < 1 || drop.Max < drop.Min)
                {
                    problems.Add($"Node '{node.Id}' drop '{drop.ItemId}' has an invalid range {drop.Min}..{drop.Max}.");
                }
            }
        }

        return problems;
    }

    private static bool HasToolAtTier(ItemDatabase items, ToolSpec required)
    {
        foreach (var item in items.All)
        {
            if (ToolRules.Of(item) is { } spec
                && string.Equals(spec.Kind, required.Kind, StringComparison.OrdinalIgnoreCase)
                && spec.Tier >= required.Tier)
            {
                return true;
            }
        }

        return false;
    }
}

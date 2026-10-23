using System.Text.Json.Serialization;

namespace Forager.Core.Nodes;

/// <summary>Wire format for <c>Data/nodes.json</c>.</summary>
internal sealed class NodeFileDto
{
    [JsonPropertyName("nodes")]
    public List<NodeDto>? Nodes { get; set; }
}

internal sealed class NodeDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("kind")] public string? Kind { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("meshPath")] public string? MeshPath { get; set; }
    [JsonPropertyName("scale")] public double Scale { get; set; } = 1;
    [JsonPropertyName("requiredTool")] public string? RequiredTool { get; set; }
    [JsonPropertyName("toolTier")] public int ToolTier { get; set; } = 1;
    [JsonPropertyName("hitPoints")] public int HitPoints { get; set; } = 1;
    [JsonPropertyName("respawnSeconds")] public double RespawnSeconds { get; set; }
    [JsonPropertyName("xp")] public int Xp { get; set; }
    [JsonPropertyName("coinChance")] public double CoinChance { get; set; }
    [JsonPropertyName("drops")] public List<DropDto>? Drops { get; set; }
}

internal sealed class DropDto
{
    [JsonPropertyName("item")] public string? Item { get; set; }
    [JsonPropertyName("min")] public int Min { get; set; } = 1;
    [JsonPropertyName("max")] public int Max { get; set; } = 1;
}

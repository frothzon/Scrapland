using System.Text.Json.Serialization;

namespace Forager.Core.Crafting;

/// <summary>Wire format for <c>Data/recipes.json</c>.</summary>
internal sealed class RecipeFileDto
{
    [JsonPropertyName("recipes")]
    public List<RecipeDto>? Recipes { get; set; }
}

internal sealed class RecipeDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("station")] public string? Station { get; set; }
    [JsonPropertyName("inputs")] public Dictionary<string, int>? Inputs { get; set; }
    [JsonPropertyName("outputs")] public Dictionary<string, int>? Outputs { get; set; }
    [JsonPropertyName("xp")] public int Xp { get; set; }
    [JsonPropertyName("unlock")] public string? Unlock { get; set; }
}

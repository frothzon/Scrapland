using System.Text.Json.Serialization;

namespace Forager.Core.Ui;

internal sealed class IconAtlasFileDto
{
    [JsonPropertyName("defaultSheet")]
    public string? DefaultSheet { get; set; }

    [JsonPropertyName("sheets")]
    public Dictionary<string, IconSheetDto>? Sheets { get; set; }

    [JsonPropertyName("icons")]
    public Dictionary<string, int>? Icons { get; set; }
}

internal sealed class IconSheetDto
{
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("cell")] public int Cell { get; set; } = 16;
    [JsonPropertyName("columns")] public int Columns { get; set; }
    [JsonPropertyName("rows")] public int Rows { get; set; } = 1;
}

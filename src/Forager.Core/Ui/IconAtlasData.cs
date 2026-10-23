using System.Text.Json;
using System.Text.Json.Serialization;
using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Core.Ui;

/// <summary>One sprite sheet with a uniform cell grid.</summary>
public sealed record IconSheet(string Path, int Cell, int Columns, int Rows)
{
    public int Capacity => Math.Max(0, Columns) * Math.Max(0, Rows);

    public bool TryRegion(int index, out int x, out int y)
    {
        if (index < 0 || index >= Capacity)
        {
            x = 0;
            y = 0;
            return false;
        }

        x = index % Columns * Cell;
        y = index / Columns * Cell;
        return true;
    }
}

/// <summary>
/// Maps logical icon names (<c>icon_wood</c>) to cells in the Sprout Lands icon sheet, so swapping an
/// icon is a data edit. Bounds are unit-tested; the visual choice is a manual eyeball check.
/// </summary>
public sealed class IconAtlasData
{
    public const string DefaultPath = "Data/icon_atlas.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private IconAtlasData(string defaultSheet, Dictionary<string, IconSheet> sheets, Dictionary<string, int> icons)
    {
        DefaultSheet = defaultSheet;
        Sheets = sheets;
        Icons = icons;
    }

    public string DefaultSheet { get; }

    public IReadOnlyDictionary<string, IconSheet> Sheets { get; }

    public IReadOnlyDictionary<string, int> Icons { get; }

    public static IconAtlasData Load(IDataSource source, string path = DefaultPath)
    {
        if (!source.Exists(path))
        {
            throw new FileNotFoundException($"Icon atlas file '{path}' was not found.", path);
        }

        return FromJson(source.ReadText(path), path);
    }

    public static IconAtlasData FromJson(string json, string origin = "<memory>")
    {
        IconAtlasFileDto? file;
        try
        {
            file = JsonSerializer.Deserialize<IconAtlasFileDto>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Icon atlas '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (file?.Sheets is null || file.Sheets.Count == 0)
        {
            throw new InvalidDataException($"Icon atlas '{origin}' has no 'sheets'.");
        }

        var sheets = new Dictionary<string, IconSheet>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in file.Sheets)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Path))
            {
                throw new InvalidDataException($"Icon atlas '{origin}' sheet '{name}' has no path.");
            }

            sheets[name] = new IconSheet(dto.Path, dto.Cell, dto.Columns, dto.Rows);
        }

        var icons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, index) in file.Icons ?? new Dictionary<string, int>())
        {
            icons[name] = index;
        }

        var defaultSheet = string.IsNullOrWhiteSpace(file.DefaultSheet) || !sheets.ContainsKey(file.DefaultSheet)
            ? sheets.Keys.First()
            : file.DefaultSheet;

        return new IconAtlasData(defaultSheet, sheets, icons);
    }

    public bool TryResolve(string iconName, out IconSheet sheet, out int x, out int y)
    {
        sheet = Sheets[DefaultSheet];
        x = 0;
        y = 0;

        if (string.IsNullOrWhiteSpace(iconName) || !Icons.TryGetValue(iconName, out var index))
        {
            return false;
        }

        return sheet.TryRegion(index, out x, out y);
    }

    /// <summary>Semantic problems in the loaded data. Empty means healthy.</summary>
    public IReadOnlyList<string> Validate(ItemDatabase items)
    {
        var problems = new List<string>();

        foreach (var (name, sheet) in Sheets)
        {
            if (sheet.Cell <= 0) problems.Add($"Icon sheet '{name}' has a non-positive cell size.");
            if (sheet.Columns <= 0) problems.Add($"Icon sheet '{name}' has non-positive columns.");
            if (sheet.Rows <= 0) problems.Add($"Icon sheet '{name}' has non-positive rows.");
            if (!sheet.Path.StartsWith("res://", StringComparison.Ordinal))
            {
                problems.Add($"Icon sheet '{name}' path must start with res:// ('{sheet.Path}').");
            }
        }

        if (!Sheets.ContainsKey(DefaultSheet))
        {
            problems.Add($"Default icon sheet '{DefaultSheet}' does not exist.");
        }

        var capacity = Sheets.TryGetValue(DefaultSheet, out var defaultSheet) ? defaultSheet.Capacity : 0;
        foreach (var (name, index) in Icons)
        {
            if (index < 0 || index >= capacity)
            {
                problems.Add($"Icon '{name}' index {index} is outside the sheet capacity {capacity}.");
            }
        }

        foreach (var item in items.All)
        {
            if (string.IsNullOrWhiteSpace(item.Icon))
            {
                problems.Add($"Item '{item.Id}' has no icon.");
            }
            else if (!Icons.ContainsKey(item.Icon))
            {
                problems.Add($"Item '{item.Id}' references unmapped icon '{item.Icon}'.");
            }
        }

        return problems;
    }
}

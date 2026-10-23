using System.Text.Json;
using System.Text.Json.Serialization;
using Forager.Core.Data;

namespace Forager.Core.Items;

/// <summary>
/// All item definitions, loaded from <c>Data/items.json</c>. Also acts as the stack-limit
/// resolver used by inventories.
/// </summary>
public sealed class ItemDatabase : IStackLimits
{
    public const string DefaultPath = "Data/items.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, ItemDefinition> _byId;

    private ItemDatabase(Dictionary<string, ItemDefinition> byId)
    {
        _byId = byId;
    }

    public IReadOnlyCollection<ItemDefinition> All => _byId.Values;

    public int Count => _byId.Count;

    public static ItemDatabase Load(IDataSource source, string path = DefaultPath)
    {
        if (!source.Exists(path))
        {
            throw new FileNotFoundException($"Item data file '{path}' was not found.", path);
        }

        return FromJson(source.ReadText(path), path);
    }

    public static ItemDatabase FromJson(string json, string origin = "<memory>")
    {
        ItemFileDto? file;
        try
        {
            file = JsonSerializer.Deserialize<ItemFileDto>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Item data '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (file?.Items is null)
        {
            throw new InvalidDataException($"Item data '{origin}' has no 'items' array.");
        }

        var byId = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in file.Items)
        {
            var id = dto.Id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidDataException($"Item data '{origin}' contains an item without an id.");
            }

            if (byId.ContainsKey(id))
            {
                throw new InvalidDataException($"Item data '{origin}' contains duplicate item id '{id}'.");
            }

            var tags = dto.Tags is null ? Array.Empty<string>() : dto.Tags.ToArray();
            byId[id] = new ItemDefinition(
                id,
                string.IsNullOrWhiteSpace(dto.Name) ? id : dto.Name!,
                dto.MaxStack <= 0 ? 99 : dto.MaxStack,
                dto.Value,
                tags,
                dto.Icon);
        }

        return new ItemDatabase(byId);
    }

    public bool TryGet(string itemId, out ItemDefinition definition) => _byId.TryGetValue(itemId, out definition!);

    public ItemDefinition Get(string itemId)
        => _byId.TryGetValue(itemId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown item id '{itemId}'.");

    public bool Contains(string itemId) => _byId.ContainsKey(itemId);

    public int MaxStack(string itemId) => TryGet(itemId, out var definition) ? definition.MaxStack : 1;

    /// <summary>Semantic problems in the loaded data. Empty means healthy. Used by tests.</summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        foreach (var definition in _byId.Values)
        {
            if (definition.Value < 0)
            {
                problems.Add($"Item '{definition.Id}' has a negative value ({definition.Value}).");
            }

            if (definition.MaxStack <= 0)
            {
                problems.Add($"Item '{definition.Id}' has a non-positive maxStack ({definition.MaxStack}).");
            }

            if (string.IsNullOrWhiteSpace(definition.Icon))
            {
                problems.Add($"Item '{definition.Id}' has no icon.");
            }
        }

        return problems;
    }

    private sealed class ItemFileDto
    {
        [JsonPropertyName("items")]
        public List<ItemDto>? Items { get; set; }
    }

    private sealed class ItemDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("maxStack")]
        public int MaxStack { get; set; } = 99;

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
    }
}

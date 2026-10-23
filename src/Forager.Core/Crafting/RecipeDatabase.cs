using System.Text.Json;
using Forager.Core.Data;

namespace Forager.Core.Crafting;

/// <summary>All recipes, loaded from <c>Data/recipes.json</c>. Validation lives in the partial file.</summary>
public sealed partial class RecipeDatabase
{
    public const string DefaultPath = "Data/recipes.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, RecipeDefinition> _byId;

    private RecipeDatabase(Dictionary<string, RecipeDefinition> byId)
    {
        _byId = byId;
    }

    public IReadOnlyCollection<RecipeDefinition> All => _byId.Values;

    public int Count => _byId.Count;

    public static RecipeDatabase Load(IDataSource source, string path = DefaultPath)
    {
        if (!source.Exists(path))
        {
            throw new FileNotFoundException($"Recipe data file '{path}' was not found.", path);
        }

        return FromJson(source.ReadText(path), path);
    }

    public static RecipeDatabase FromJson(string json, string origin = "<memory>")
    {
        RecipeFileDto? file;
        try
        {
            file = JsonSerializer.Deserialize<RecipeFileDto>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Recipe data '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (file?.Recipes is null)
        {
            throw new InvalidDataException($"Recipe data '{origin}' has no 'recipes' array.");
        }

        var byId = new Dictionary<string, RecipeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in file.Recipes)
        {
            var id = dto.Id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidDataException($"Recipe data '{origin}' contains a recipe without an id.");
            }

            if (byId.ContainsKey(id))
            {
                throw new InvalidDataException($"Recipe data '{origin}' contains duplicate recipe id '{id}'.");
            }

            if (!StationKinds.TryParse(dto.Station, out var station))
            {
                throw new InvalidDataException($"Recipe '{id}' has unknown station '{dto.Station}'.");
            }

            var inputs = ToAmounts(dto.Inputs, id, "inputs");
            var outputs = ToAmounts(dto.Outputs, id, "outputs");
            var (unlock, unlockError) = ParseUnlock(dto.Unlock);

            byId[id] = new RecipeDefinition(id, station, inputs, outputs, dto.Xp, unlock, unlockError);
        }

        return new RecipeDatabase(byId);
    }

    public bool TryGet(string recipeId, out RecipeDefinition definition) => _byId.TryGetValue(recipeId, out definition!);

    public RecipeDefinition Get(string recipeId)
        => _byId.TryGetValue(recipeId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown recipe id '{recipeId}'.");

    public bool Contains(string recipeId) => _byId.ContainsKey(recipeId);

    public IReadOnlyList<RecipeDefinition> ForStation(StationKind station)
        => _byId.Values
            .Where(recipe => recipe.Station == station)
            .OrderBy(recipe => recipe.Id, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<ItemAmount> ToAmounts(Dictionary<string, int>? source, string recipeId, string side)
    {
        if (source is null || source.Count == 0)
        {
            throw new InvalidDataException($"Recipe '{recipeId}' has no '{side}'.");
        }

        return source
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Select(pair => new ItemAmount(pair.Key.Trim().ToLowerInvariant(), pair.Value))
            .OrderBy(amount => amount.ItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private static (RecipeUnlock Unlock, string? Error) ParseUnlock(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (RecipeUnlock.Always, null);
        }

        var text = raw.Trim();

        const string levelPrefix = "level:";
        if (text.StartsWith(levelPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(text[levelPrefix.Length..].Trim(), out var level) && level >= 1
                ? (RecipeUnlock.AtLevel(level), null)
                : (RecipeUnlock.Always, $"'{raw}' is not a valid level unlock");
        }

        const string afterPrefix = "after:";
        if (text.StartsWith(afterPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var target = text[afterPrefix.Length..].Trim();
            return target.Length > 0
                ? (RecipeUnlock.AfterRecipe(target.ToLowerInvariant()), null)
                : (RecipeUnlock.Always, $"'{raw}' is missing a recipe id");
        }

        return (RecipeUnlock.Always, $"'{raw}' is not a known unlock (use level:N or after:<recipe>)");
    }
}

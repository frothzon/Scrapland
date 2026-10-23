using System.Text.Json;
using System.Text.Json.Serialization;
using Forager.Core.Player;
using Forager.Core.Sim;

namespace Forager.Core.Save;

/// <summary>JSON (de)serialisation plus capture/restore of live sessions.</summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string ToJson(SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(save);
        save.Version = SaveGame.CurrentVersion;
        return JsonSerializer.Serialize(save, JsonOptions);
    }

    public static SaveGame FromJson(string json, string origin = "<memory>")
    {
        SaveGame? save;
        try
        {
            save = JsonSerializer.Deserialize<SaveGame>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Save data '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (save is null)
        {
            throw new InvalidDataException($"Save data '{origin}' is empty.");
        }

        if (save.Version < 1)
        {
            throw new InvalidDataException($"Save data '{origin}' has an invalid version ({save.Version}).");
        }

        if (save.Version > SaveGame.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Save data '{origin}' uses version {save.Version}, but this build supports up to {SaveGame.CurrentVersion}.");
        }

        save.Slots ??= [];
        save.Nodes ??= [];
        save.Level = Math.Max(1, save.Level);
        save.Xp = Math.Max(0, save.Xp);
        save.Coins = Math.Max(0, save.Coins);
        save.SimSeconds = Math.Max(0, save.SimSeconds);

        return save;
    }

    /// <summary>Snapshots the parts of a live session that M1 persists.</summary>
    public static SaveGame Capture(PlayerProfile profile, SimClock clock, int seed)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(clock);

        var save = new SaveGame
        {
            Seed = seed,
            SimSeconds = clock.TotalSeconds,
            Level = profile.Level,
            Xp = profile.Xp,
            TotalXp = profile.TotalXp,
            Coins = profile.Coins,
        };

        for (var i = 0; i < profile.Inventory.SlotCount; i++)
        {
            var slot = profile.Inventory.GetSlot(i);
            if (!slot.IsEmpty)
            {
                save.Slots.Add(new SavedSlot { ItemId = slot.ItemId, Amount = slot.Amount });
            }
        }

        return save;
    }

    /// <summary>Applies a save file to a live profile and clock.</summary>
    public static void Restore(SaveGame save, PlayerProfile profile, SimClock clock)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(clock);

        profile.Inventory.Clear();
        foreach (var slot in save.Slots)
        {
            if (!slot.IsEmpty() && slot.Amount > 0)
            {
                profile.Inventory.Add(slot.ItemId, slot.Amount);
            }
        }

        profile.Restore(save.Level, save.Xp, save.Coins, save.TotalXp);
        clock.RestoreTo(save.SimSeconds);
    }

    private static bool IsEmpty(this SavedSlot slot)
        => slot is null || string.IsNullOrWhiteSpace(slot.ItemId);
}

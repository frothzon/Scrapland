using Forager.Core.Crafting;
using ForagerClone.Crafting;
using Godot;

namespace ForagerClone.Game;

/// <summary>
/// Crafting side of the session: recipe data, the recipe book, station proximity and the UI-open flag.
/// Panels call into here; nothing here talks to the UI.
/// </summary>
public partial class GameSession
{
    public RecipeDatabase Recipes { get; private set; } = null!;

    public RecipeBook Book { get; } = new();

    public CraftingService Crafting { get; private set; } = null!;

    public IReadOnlyList<Station3D> WorldStations { get; private set; } = Array.Empty<Station3D>();

    /// <summary>Station whose recipes are shown in the craft panel (null = hand recipes only).</summary>
    public StationKind? ActiveStation { get; private set; }

    /// <summary>True while a panel owns the input focus (movement and harvesting are suspended).</summary>
    public bool UiOpen { get; private set; }

    public void RegisterStations(IReadOnlyList<Station3D> stations) => WorldStations = stations;

    /// <summary>Nearest station within its interact radius, or null.</summary>
    public Station3D? NearbyStation(Vector3 point)
    {
        Station3D? best = null;
        var bestDistance = double.MaxValue;

        foreach (var station in WorldStations)
        {
            var distance = station.GlobalPosition.DistanceTo(point);
            if (distance <= station.Radius && distance < bestDistance)
            {
                best = station;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>Opens the craft panel, optionally bound to a station.</summary>
    public void OpenCrafting(StationKind? station)
    {
        ActiveStation = station;
        UiOpen = true;
        StatusMessage = station is { } kind ? $"Crafting at {kind}" : "Hand crafting";
    }

    public void OpenInventory() => UiOpen = true;

    public void CloseUi()
    {
        UiOpen = false;
        ActiveStation = null;
    }

    /// <summary>Crafts through the active station and applies xp on success.</summary>
    public CraftOutcome Craft(string recipeId)
    {
        if (!IsBooted)
        {
            return CraftOutcome.Fail(CraftStatus.UnknownRecipe);
        }

        var outcome = Crafting.Craft(recipeId, Profile.Inventory, Book, Profile.Level, ActiveStation);
        if (outcome.Success)
        {
            Profile.AddXp(outcome.Xp);
            StatusMessage = $"Crafted {outcome.Recipe!.Id}";
        }
        else
        {
            StatusMessage = outcome.Message;
        }

        return outcome;
    }

    /// <summary>Recipes the panel should list right now (hand + active station).</summary>
    public IReadOnlyList<RecipeDefinition> VisibleRecipes() => Crafting.RecipesForStation(ActiveStation);

    public string? LockReason(RecipeDefinition recipe) => Book.LockReason(recipe, Profile.Level);

    public bool CanCraft(RecipeDefinition recipe)
        => Crafting.CanCraft(recipe, Profile.Inventory, Book, Profile.Level, ActiveStation);
}

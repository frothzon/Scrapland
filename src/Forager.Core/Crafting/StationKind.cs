namespace Forager.Core.Crafting;

/// <summary>Where a recipe can be crafted. <c>Hand</c> works anywhere.</summary>
public enum StationKind
{
    Hand,
    Workbench,
    Furnace,
}

public static class StationKinds
{
    public static bool TryParse(string? value, out StationKind kind)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "hand":
                kind = StationKind.Hand;
                return true;
            case "workbench":
                kind = StationKind.Workbench;
                return true;
            case "furnace":
                kind = StationKind.Furnace;
                return true;
            default:
                kind = StationKind.Hand;
                return false;
        }
    }

    public static string ToDataString(StationKind kind) => kind switch
    {
        StationKind.Workbench => "workbench",
        StationKind.Furnace => "furnace",
        _ => "hand",
    };

    public static IReadOnlyList<StationKind> All { get; } =
        [StationKind.Hand, StationKind.Workbench, StationKind.Furnace];
}

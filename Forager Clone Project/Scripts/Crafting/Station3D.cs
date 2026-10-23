using Forager.Core.Crafting;
using Godot;

namespace ForagerClone.Crafting;

/// <summary>
/// A crafting station in the world. Purely physical/presentational: the crafting rules live in
/// <c>Forager.Core.Crafting.CraftingService</c>.
/// </summary>
public partial class Station3D : StaticBody3D
{
    public StationKind Kind { get; private set; } = StationKind.Workbench;

    /// <summary>How close the player must stand to use it (from <c>Data/balance.json</c>).</summary>
    public double Radius { get; private set; } = 3.0;

    public string DisplayName => Kind == StationKind.Furnace ? "Furnace" : "Workbench";

    public int OpenCount { get; private set; }

    private Tween? _pulse;

    public void Initialize(StationKind kind, double radius)
    {
        Kind = kind;
        Radius = Math.Max(0.5, radius);
    }

    public bool IsInRange(Vector3 point) => GlobalPosition.DistanceTo(point) <= Radius;

    public void Pulse()
    {
        OpenCount++;
        if (VisualRoot is null)
        {
            return;
        }

        _pulse?.Kill();
        VisualRoot.Scale = Vector3.One * 1.08f;
        _pulse = CreateTween();
        _pulse.TweenProperty(VisualRoot, "scale", Vector3.One, 0.15);
    }

    public Node3D? VisualRoot { get; set; }
}

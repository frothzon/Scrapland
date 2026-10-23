using Forager.Core.Crafting;
using Godot;

namespace ForagerClone.Crafting;

/// <summary>
/// Builds station props in code (same approach as <c>NodeFactory</c>): a coloured box, a collider and a
/// billboard label. M3 will replace the box with a vendor model once building exists.
/// </summary>
public static class StationFactory
{
    public const int InteractableLayer = 1 << 2;

    public static Station3D Create(StationKind kind, double radius, Vector3 position)
    {
        var station = new Station3D
        {
            Name = $"Station_{kind}",
            Position = position,
            CollisionLayer = InteractableLayer,
            CollisionMask = 0,
        };

        var size = new Vector3(1.2f, 0.8f, 1.2f);
        var color = kind == StationKind.Furnace
            ? new Color(0.55f, 0.32f, 0.26f)
            : new Color(0.64f, 0.44f, 0.25f);

        var pivot = new Node3D { Name = "Pivot" };
        pivot.AddChild(new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = 1f,
                Metallic = 0f,
            },
            Position = new Vector3(0, size.Y / 2f, 0),
        });
        station.AddChild(pivot);

        station.AddChild(new CollisionShape3D
        {
            Name = "Collision",
            Shape = new BoxShape3D { Size = size },
            Position = new Vector3(0, size.Y / 2f, 0),
        });

        station.AddChild(new Label3D
        {
            Name = "Label",
            Text = kind == StationKind.Furnace ? "Furnace" : "Workbench",
            Position = new Vector3(0, size.Y + 0.55f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 48,
            OutlineSize = 8,
            Modulate = new Color(1f, 0.97f, 0.9f),
        });

        station.VisualRoot = pivot;
        station.Initialize(kind, radius);
        return station;
    }
}

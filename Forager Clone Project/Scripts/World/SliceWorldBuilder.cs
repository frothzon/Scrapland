using Forager.Core.Config;
using Forager.Core.Nodes;
using Forager.Core.World;
using ForagerClone.Game;
using ForagerClone.Nodes;
using Godot;

namespace ForagerClone.World;

/// <summary>
/// Builds the M1 slice: one plot of harvestable nodes placed deterministically from the seed. Placement
/// maths lives in Core (<see cref="PlotPlacement"/>) so the layout is unit-tested.
/// </summary>
public static class SliceWorldBuilder
{
    /// <summary>Flat plot floor with a real collider so the player can stand on it.</summary>
    public static StaticBody3D BuildGround(double plotSize)
    {
        const float thickness = 0.4f;
        var size = new Vector3((float)plotSize, thickness, (float)plotSize);
        var offset = new Vector3(0, -thickness / 2f, 0);

        var ground = new StaticBody3D { Name = "Ground", CollisionLayer = 1, CollisionMask = 0 };
        ground.AddChild(new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.42f, 0.72f, 0.36f),
                Roughness = 1f,
                MetallicSpecular = 0.1f,
            },
            Position = offset,
        });
        ground.AddChild(new CollisionShape3D
        {
            Name = "Collision",
            Shape = new BoxShape3D { Size = size },
            Position = offset,
        });

        return ground;
    }

    public static IReadOnlyList<ResourceNode3D> Build(
        Node3D root,
        NodeDatabase nodes,
        BalanceConfig balance,
        GameSession session,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(balance);
        ArgumentNullException.ThrowIfNull(session);

        var definitions = nodes.All.OrderBy(definition => definition.Id, StringComparer.Ordinal).ToList();
        if (definitions.Count == 0)
        {
            return Array.Empty<ResourceNode3D>();
        }

        var placed = PlotPlacement.Generate(definitions, balance.World.NodeCount, balance.World.PlotSize, seed);
        var created = new List<ResourceNode3D>(placed.Count);

        foreach (var spot in placed)
        {
            var definition = nodes.Get(spot.DefinitionId);
            var mesh = GD.Load<Mesh>(definition.MeshPath);
            if (mesh is null)
            {
                GD.PushError($"SliceWorldBuilder: cannot load mesh '{definition.MeshPath}' for node '{definition.Id}'.");
                continue;
            }

            var node = NodeFactory.Create(definition, mesh, session, session.Clock);
            node.Position = new Vector3((float)spot.X, 0f, (float)spot.Z);
            node.RotationDegrees = new Vector3(0, (float)(Math.Abs(spot.X + spot.Z) * 47d % 360d), 0);

            root.AddChild(node);
            created.Add(node);
        }

        return created;
    }
}

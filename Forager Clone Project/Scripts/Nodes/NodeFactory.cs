using Forager.Core.Nodes;
using Forager.Core.Sim;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Nodes;

/// <summary>
/// Builds a harvestable node from data. Meshes are untextured OBJ imports whose origins are not centred
/// (LL: asset notes), so the pivot re-centres them and drops the bottom to y = 0, and the collider is
/// derived from the mesh bounds instead of being hand-tuned per asset.
/// </summary>
public static class NodeFactory
{
    public const int InteractableLayer = 1 << 2;

    public static ResourceNode3D Create(
        NodeDefinition definition,
        Mesh mesh,
        GameSession? session = null,
        SimClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(mesh);

        var node = new ResourceNode3D
        {
            Name = $"Node_{definition.Id}",
            CollisionLayer = InteractableLayer,
            CollisionMask = 0,
            Scale = Vector3.One * (float)definition.Scale,
        };

        var aabb = mesh.GetAabb();
        var pivot = new Node3D { Name = "Pivot" };
        var meshInstance = new MeshInstance3D
        {
            Name = "Mesh",
            Mesh = mesh,
            Position = new Vector3(
                -(aabb.Position.X + (aabb.Size.X / 2f)),
                -aabb.Position.Y,
                -(aabb.Position.Z + (aabb.Size.Z / 2f))),
        };

        pivot.AddChild(meshInstance);
        node.AddChild(pivot);

        var shape = new CylinderShape3D
        {
            Radius = Math.Max(0.3f, Math.Max(aabb.Size.X, aabb.Size.Z) * 0.35f),
            Height = Math.Max(0.5f, aabb.Size.Y),
        };

        node.AddChild(new CollisionShape3D
        {
            Name = "Collision",
            Shape = shape,
            Position = new Vector3(0, shape.Height / 2f, 0),
        });

        node.VisualRoot = pivot;
        node.MeshNode = meshInstance;
        node.Initialize(definition, clock, session);

        return node;
    }
}

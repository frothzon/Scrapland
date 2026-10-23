using Forager.Core.Player;
using Godot;

namespace ForagerClone.Items;

/// <summary>
/// A dropped item. It is magnetised towards the player inside <see cref="MagnetRadius"/> and collected
/// into the Core inventory when close enough. If the inventory is full it stays on the ground.
/// </summary>
public partial class Pickup3D : Node3D
{
    public const double CollectDistance = 0.55;

    public string ItemId { get; set; } = string.Empty;

    public int Amount { get; set; } = 1;

    public Node3D? Target { get; set; }

    public PlayerProfile? Profile { get; set; }

    public double MagnetRadius { get; set; } = 3.0;

    public double MagnetSpeed { get; set; } = 9.0;

    public bool IsCollected { get; private set; }

    public event Action<string, int>? Collected;

    public override void _Ready()
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.85f, 0.35f),
            AlbedoTexture = null,
            Metallic = 0f,
            Roughness = 1f,
        };

        var mesh = new SphereMesh { Radius = 0.14f, Height = 0.28f, Material = material };
        AddChild(new MeshInstance3D { Name = "Visual", Mesh = mesh });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsCollected || Target is null)
        {
            return;
        }

        var toTarget = (Target.GlobalPosition + (Vector3.Up * 0.8f)) - GlobalPosition;
        var distance = toTarget.Length();

        if (distance <= CollectDistance)
        {
            Collect();
            return;
        }

        if (distance > MagnetRadius)
        {
            return;
        }

        var step = Math.Min(MagnetSpeed * delta, distance);
        GlobalPosition += toTarget.Normalized() * (float)step;
    }

    /// <summary>Adds the drop to the inventory. Returns false when it did not fit.</summary>
    public bool Collect()
    {
        if (IsCollected || Profile is null)
        {
            return false;
        }

        var leftover = Profile.Inventory.Add(ItemId, Amount);
        if (leftover > 0)
        {
            return false;
        }

        IsCollected = true;
        Collected?.Invoke(ItemId, Amount);
        QueueFree();
        return true;
    }
}

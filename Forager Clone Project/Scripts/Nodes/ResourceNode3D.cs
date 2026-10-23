using Forager.Core.Gathering;
using Forager.Core.Nodes;
using Forager.Core.Sim;
using ForagerClone.Game;
using ForagerClone.Interaction;
using ForagerClone.Items;
using Godot;

namespace ForagerClone.Nodes;

/// <summary>
/// A harvestable world node. Pure presentation + state mirror: all rules (tool gate, cooldown, drops)
/// live in <see cref="HarvestService"/>; this node only shows the result and spawns pickups.
/// </summary>
public partial class ResourceNode3D : StaticBody3D, IInteractable
{
    public NodeDefinition Definition { get; private set; } = null!;

    public ResourceNodeState State { get; private set; } = null!;

    public Node3D Anchor => this;

    public Node3D? VisualRoot { get; set; }

    public MeshInstance3D? MeshNode { get; set; }

    public GameSession? Session { get; set; }

    public SimClock? Clock { get; set; }

    public int HitCount { get; private set; }

    public int RespawnCount { get; private set; }

    public event Action? Respawned;

    private Tween? _hitTween;

    public void Initialize(NodeDefinition definition, SimClock? clock = null, GameSession? session = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Clock = clock;
        Session = session;
        State = new ResourceNodeState(definition);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (State is null)
        {
            return;
        }

        var now = Clock?.TotalSeconds ?? 0;
        if (State.RespawnIfDue(now))
        {
            RespawnCount++;
            ShowAliveState();
            Respawned?.Invoke();
        }
    }

    public void ShowHitFeedback(bool felled)
    {
        HitCount++;
        if (felled)
        {
            ShowDepletedState();
        }

        if (VisualRoot is null)
        {
            return;
        }

        _hitTween?.Kill();
        if (felled)
        {
            return;
        }

        VisualRoot.Scale = Vector3.One * 0.9f;
        _hitTween = CreateTween();
        _hitTween.TweenProperty(VisualRoot, "scale", Vector3.One, 0.18);
    }

    public void ShowDepletedState()
    {
        if (MeshNode is not null)
        {
            MeshNode.Visible = false;
        }

        CollisionLayer = 0;
    }

    public void ShowAliveState()
    {
        if (MeshNode is not null)
        {
            MeshNode.Visible = true;
        }

        CollisionLayer = 1 << 2;
    }

    public void SpawnDrops(IReadOnlyList<HarvestDrop> drops)
    {
        if (Session is null || drops.Count == 0)
        {
            return;
        }

        var parent = GetParent() ?? this;
        foreach (var drop in drops)
        {
            var pickup = new Pickup3D
            {
                Name = $"Pickup_{drop.ItemId}",
                ItemId = drop.ItemId,
                Amount = drop.Amount,
                Target = Session.Player,
                Profile = Session.Profile,
                MagnetRadius = Session.Balance.Gather.MagnetRadius,
                MagnetSpeed = Session.Balance.Gather.MagnetSpeed,
                Position = GlobalPosition + new Vector3(0, 0.8f, 0),
            };

            parent.AddChild(pickup);
        }
    }

    /// <summary>Applies a saved node state (used by load).</summary>
    public void RestoreState(int hitPointsLeft, double respawnAtSeconds)
    {
        State = ResourceNodeState.Restore(Definition, hitPointsLeft, respawnAtSeconds);
        if (State.IsDepleted)
        {
            ShowDepletedState();
        }
        else
        {
            ShowAliveState();
        }
    }
}

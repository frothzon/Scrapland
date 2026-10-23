using Forager.Core.Gathering;
using ForagerClone.Crafting;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Interaction;

/// <summary>
/// Aim + harvest driver. A RayCast3D is cast from the camera along its forward axis; holding
/// <c>use_tool</c> (LMB) or <c>interact</c> (E) harvests the current target.
/// Tests bypass the mouse with <see cref="AimAt"/> + <see cref="TryHarvestCurrentTarget"/>.
/// </summary>
public partial class Interactor : Node3D
{
    public GameSession? Session { get; set; }

    public Camera3D? AimCamera { get; set; }

    public RayCast3D Ray { get; private set; } = null!;

    public IInteractable? CurrentTarget { get; private set; }

    /// <summary>Raw collider under the aim ray (may be a station, which is not harvestable).</summary>
    public Node3D? CurrentCollider { get; private set; }

    /// <summary>Contextual hint for the HUD (e.g. "E: Workbench"), empty when there is none.</summary>
    public string Prompt { get; private set; } = string.Empty;

    /// <summary>Feedback for the HUD ("Needs a wood_axe", "Depleted", …).</summary>
    public string Message { get; private set; } = string.Empty;

    public HarvestOutcome? LastOutcome { get; private set; }

    public int HarvestCount { get; private set; }

    /// <summary>When true the camera no longer drives the ray, so a test can aim deterministically.</summary>
    public bool ManualAim { get; private set; }

    /// <summary>How far the player can reach, measured from the player (not from the camera).</summary>
    public double MaxReachMeters { get; private set; } = 12;

    /// <summary>Ray length from the camera; generous because the camera sits far behind the player.</summary>
    public double RayLengthMeters { get; private set; } = 45;

    public override void _Ready() => EnsureRay();

    /// <summary>Creates the ray lazily so tests can use the interactor before its first frame.</summary>
    private void EnsureRay()
    {
        if (Ray is not null)
        {
            return;
        }

        Ray = new RayCast3D
        {
            Name = "AimRay",
            Enabled = true,
            TargetPosition = new Vector3(0, 0, (float)-RayLengthMeters),
            CollisionMask = 1 << 2,
            CollideWithBodies = true,
            CollideWithAreas = false,
        };
        AddChild(Ray);

        // Never hit the player carrying this interactor.
        if (GetParent() is CollisionObject3D owner)
        {
            Ray.AddException(owner);
        }
    }

    public void Configure(double reachMeters)
    {
        MaxReachMeters = Math.Max(0.1, reachMeters);
        RayLengthMeters = Math.Max(40, MaxReachMeters + 25);
        if (Ray is not null)
        {
            Ray.TargetPosition = new Vector3(0, 0, (float)-RayLengthMeters);
        }
    }

    /// <summary>Points the ray at a world position (test/diagnostic seam).</summary>
    public void AimAt(Vector3 worldPoint)
    {
        ManualAim = true;
        EnsureRay();
        var origin = AimCamera?.GlobalPosition ?? GlobalPosition;
        Ray.GlobalPosition = origin;
        if (origin.DistanceTo(worldPoint) > 0.001f)
        {
            Ray.LookAt(worldPoint, Vector3.Up);
        }

        RefreshTarget();
    }

    public void UseCameraAim() => ManualAim = false;

    public void RefreshTarget()
    {
        EnsureRay();
        Ray.ForceRaycastUpdate();

        CurrentCollider = Ray.GetCollider() as Node3D;

        var harvestable = CurrentCollider as IInteractable;
        CurrentTarget = harvestable is not null && IsWithinReach(harvestable) ? harvestable : null;

        Prompt = CurrentCollider switch
        {
            Station3D station when IsStationInReach(station) => $"E: {station.DisplayName}",
            _ => string.Empty,
        };
    }

    private bool IsStationInReach(Station3D station)
    {
        var player = Session?.Player;
        return player is null || station.GlobalPosition.DistanceTo(player.GlobalPosition) <= MaxReachMeters;
    }

    private bool IsWithinReach(IInteractable target)
    {
        var player = Session?.Player;
        if (player is null)
        {
            return true;
        }

        return target.Anchor.GlobalPosition.DistanceTo(player.GlobalPosition) <= MaxReachMeters;
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsureRay();

        if (!ManualAim && AimCamera is not null)
        {
            Ray.GlobalTransform = new Transform3D(AimCamera.GlobalTransform.Basis, AimCamera.GlobalPosition);
            Ray.TargetPosition = new Vector3(0, 0, (float)-RayLengthMeters);
        }

        RefreshTarget();

        if (Session is null || !Session.IsBooted)
        {
            return;
        }

        if (Session.UiOpen)
        {
            return;
        }

        if (Input.IsActionJustPressed(InputActions.Interact))
        {
            TryInteractCurrentTarget();
            return;
        }

        if (Input.IsActionPressed(InputActions.Use))
        {
            TryHarvestCurrentTarget();
        }
    }

    /// <summary>
    /// E / context action: opens a station when one is aimed at, otherwise swings at a node.
    /// Returns true when something happened.
    /// </summary>
    public bool TryInteractCurrentTarget()
    {
        if (Session is null || !Session.IsBooted || Session.UiOpen)
        {
            return false;
        }

        RefreshTarget();

        if (CurrentCollider is Station3D station && IsStationInReach(station))
        {
            station.Pulse();
            Session.OpenCrafting(station.Kind);
            Message = $"{station.DisplayName} opened";
            return true;
        }

        return TryHarvestCurrentTarget()?.Success == true;
    }

    /// <summary>Harvests the current target once. Returns null when there is nothing to harvest.</summary>
    public HarvestOutcome? TryHarvestCurrentTarget()
    {
        if (Session is null || !Session.IsBooted || Session.UiOpen)
        {
            return null;
        }

        RefreshTarget();
        var target = CurrentTarget;
        if (target is null)
        {
            Message = string.Empty;
            return null;
        }

        var outcome = Session.Harvest.TryHarvest(
            target.Definition,
            target.State,
            Session.ToolFor(target.Definition.RequiredTool),
            Session.NowSeconds);

        LastOutcome = outcome;

        switch (outcome.Status)
        {
            case HarvestStatus.MissingTool:
                Message = $"Needs {target.Definition.RequiredTool}";
                break;

            case HarvestStatus.Depleted:
                Message = $"{target.Definition.DisplayName} depleted";
                break;

            case HarvestStatus.OnCooldown:
                break;

            case HarvestStatus.Success:
                HarvestCount++;
                target.ShowHitFeedback(outcome.Felled);
                if (outcome.Felled)
                {
                    Session.Profile.AddXp(outcome.Xp);
                    Session.Profile.AddCoins(outcome.Coins);
                    target.SpawnDrops(outcome.Drops);
                    Message = $"+{outcome.Xp} xp";
                }
                else
                {
                    Message = string.Empty;
                }

                break;
        }

        return outcome;
    }
}

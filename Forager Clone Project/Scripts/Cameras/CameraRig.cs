using Forager.Core.CameraControl;
using Forager.Core.Config;
using Godot;

namespace ForagerClone.Cameras;

/// <summary>
/// Fixed angled follow camera: constant yaw/pitch/distance, smooth follow, no mouse look. Its yaw is the
/// basis for camera-relative movement, so tests can rotate it and assert the movement direction.
/// </summary>
public partial class CameraRig : Node3D
{
    public CameraTuning Tuning { get; set; } = new(0, -55, 14, 1.0, 12);

    public double YawDegrees { get; private set; }

    public Node3D? Target { get; set; }

    public Camera3D Camera { get; private set; } = null!;

    public override void _Ready()
    {
        Camera = GetNodeOrNull<Camera3D>("Camera3D") ?? CreateCamera();
        SnapToTarget();
    }

    public void Configure(CameraTuning tuning)
    {
        Tuning = tuning;
        YawDegrees = tuning.YawDegrees;
        SnapToTarget();
    }

    public void SetYawDegrees(double yawDegrees)
    {
        YawDegrees = yawDegrees;
        SnapToTarget();
    }

    public Vector3 FocusPoint => Target is null
        ? GlobalPosition
        : Target.GlobalPosition + (Vector3.Up * (float)Tuning.TargetHeight);

    public Vector3 DesiredPosition
    {
        get
        {
            var offset = CameraMath.OffsetFor(Tuning.PitchDegrees, Tuning.Distance);
            var local = new Vector3(0, (float)offset.Y, (float)offset.Z);
            var rotated = local.Rotated(Vector3.Up, (float)(YawDegrees * CameraMath.DegreesToRadians));
            return FocusPoint + rotated;
        }
    }

    public void SnapToTarget()
    {
        if (Target is null || Camera is null)
        {
            return;
        }

        GlobalPosition = DesiredPosition;
        if (GlobalPosition.DistanceTo(FocusPoint) > 0.01f)
        {
            Camera.LookAt(FocusPoint, Vector3.Up);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target is null)
        {
            return;
        }

        var focus = FocusPoint;
        var desired = DesiredPosition;
        var weight = (float)Math.Clamp(Tuning.FollowSharpness * delta, 0, 1);
        GlobalPosition = GlobalPosition.Lerp(desired, weight);

        if (GlobalPosition.DistanceTo(focus) > 0.01f)
        {
            Camera.LookAt(focus, Vector3.Up);
        }
    }

    /// <summary>Camera height above the focus point (tests assert the fixed pitch geometry).</summary>
    public double HeightAboveFocus => GlobalPosition.Y - FocusPoint.Y;

    private Camera3D CreateCamera()
    {
        var camera = new Camera3D { Name = "Camera3D", Fov = 35 };
        AddChild(camera);
        camera.Current = true;
        return camera;
    }
}

using Forager.Core.CameraControl;
using Forager.Core.Config;
using ForagerClone.Cameras;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Player;

/// <summary>
/// Camera-relative character controller. Real input and headless tests drive the same
/// <see cref="ApplyMoveInput"/> seam, so a test can prove movement without a keyboard.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    [Export] public NodePath? CameraRigPath { get; set; }

    [Export] public float CapsuleRadius { get; set; } = 0.3f;

    public PlayerTuning Tuning { get; set; } = new(3.5, 6.0, 30, 5.0, 22, 0.73);

    public CameraRig? Rig { get; set; }

    /// <summary>Last world-space movement direction (unit length or zero).</summary>
    public Vector3 MoveDirection { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsInjectedInput { get; private set; }

    private Vector2 _input;
    private bool _run;
    private bool _jumpQueued;

    /// <summary>
    /// Feeds input in Godot's convention: x = right − left, y = back − forward (so "forward" is y = −1).
    /// </summary>
    public void ApplyMoveInput(Vector2 input, bool run, bool jump = false)
    {
        _input = input;
        _run = run;
        IsInjectedInput = true;
        if (jump)
        {
            _jumpQueued = true;
        }
    }

    public void ClearInjectedInput() => IsInjectedInput = false;

    public override void _Ready()
    {
        InputActions.EnsureRegistered();

        if (Rig is null && CameraRigPath is not null)
        {
            Rig = GetNodeOrNull<CameraRig>(CameraRigPath);
        }

        if (Rig is null)
        {
            Rig = GetTree().Root.FindChild("CameraRig", true, false) as CameraRig;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = IsInjectedInput
            ? _input
            : Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveForward, InputActions.MoveBack);

        var running = IsInjectedInput ? _run : Input.IsActionPressed(InputActions.Run);
        IsRunning = running;

        var yaw = Rig?.YawDegrees ?? 0;
        var (dx, dz) = CameraMath.MoveDirection(input.X, -input.Y, yaw);
        MoveDirection = new Vector3((float)dx, 0, (float)dz);

        var speed = running ? Tuning.RunSpeed : Tuning.WalkSpeed;
        var target = new Vector3(MoveDirection.X * (float)speed, 0, MoveDirection.Z * (float)speed);
        var horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(target, (float)(Tuning.Acceleration * delta));

        var vertical = Velocity.Y;
        if (IsOnFloor())
        {
            if (vertical < 0)
            {
                vertical = 0;
            }

            if (_jumpQueued || (!IsInjectedInput && Input.IsActionJustPressed(InputActions.Jump)))
            {
                vertical = (float)Tuning.JumpVelocity;
            }
                else
            {
                vertical = Math.Min(vertical, 0f);
            }
        }
        else
        {
            vertical -= (float)(Tuning.Gravity * delta);
        }

        _jumpQueued = false;
        Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        MoveAndSlide();
    }

    /// <summary>Turns the visible model towards movement (presentation only).</summary>
    public void FaceMovement(double delta)
    {
        var anchor = GetNodeOrNull<Node3D>("ModelAnchor");
        if (anchor is null || MoveDirection.LengthSquared() < 0.0001f)
        {
            return;
        }

        var targetYaw = Mathf.Atan2(MoveDirection.X, MoveDirection.Z);
        anchor.Rotation = new Vector3(
            anchor.Rotation.X,
            Mathf.LerpAngle(anchor.Rotation.Y, targetYaw, Mathf.Clamp((float)(10 * delta), 0, 1)),
            anchor.Rotation.Z);
    }
}

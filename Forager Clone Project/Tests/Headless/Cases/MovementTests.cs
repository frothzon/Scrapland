using ForagerClone.Cameras;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Movement is camera-relative and the camera geometry stays fixed.</summary>
[HeadlessTest(Tags = "player,movement,camera")]
public partial class MovementTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var player = fixture.Player;

        await context.StepPhysics(3);

        // --- walk one second -------------------------------------------------
        player.ApplyMoveInput(new Vector2(0, -1), run: false);
        var start = player.GlobalPosition;
        await context.StepPhysics(60);
        var walked = new Vector2(player.GlobalPosition.X - start.X, player.GlobalPosition.Z - start.Z).Length();

        context.AssertInRange(walked, 3.0, 3.7, $"one second of walking covers ~3.5 m (got {walked:0.00})");
        context.AssertTrue(player.GlobalPosition.Z < start.Z - 3f, "forward input moves toward -Z at yaw 0");
        context.AssertNear(0, player.GlobalPosition.X, 0.05, "no sideways drift");

        // --- run is faster ---------------------------------------------------
        player.GlobalPosition = Vector3.Zero;
        player.Velocity = Vector3.Zero;
        player.ApplyMoveInput(new Vector2(0, -1), run: true);
        var runStart = player.GlobalPosition;
        await context.StepPhysics(60);
        var ran = new Vector2(player.GlobalPosition.X - runStart.X, player.GlobalPosition.Z - runStart.Z).Length();

        context.AssertTrue(ran > walked + 1.0, $"running ({ran:0.00} m) must beat walking ({walked:0.00} m)");

        // --- fixed camera geometry -------------------------------------------
        player.ClearInjectedInput();
        await context.StepPhysics(90);

        var rig = fixture.Rig;
        var distance = rig.GlobalPosition.DistanceTo(rig.FocusPoint);
        context.AssertNear(14, distance, 0.3, $"camera keeps its 14 m distance (got {distance:0.00})");
        context.AssertNear(11.47, rig.HeightAboveFocus, 0.3, "camera keeps its fixed height above the player");

        var forward = -rig.Camera.GlobalTransform.Basis.Z.Normalized();
        var pitch = Mathf.RadToDeg(Mathf.Asin(forward.Y));
        context.AssertNear(-55, pitch, 2.0, $"camera looks down at -55 degrees (got {pitch:0.0})");
    }
}

/// <summary>With the rig yawed 90°, "forward" must become world -X.</summary>
[HeadlessTest(Tags = "camera,movement")]
public partial class CameraRelativeMoveTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this, seed: 99);

        context.AssertNotNull(fixture.Player.Rig, "the player is wired to a camera rig");
        context.AssertEqual(fixture.Rig, fixture.Player.Rig, "the player uses the fixture's rig (explicit injection, not a name lookup)");

        fixture.Rig.SetYawDegrees(90);
        await context.StepPhysics(2);

        fixture.Player.ApplyMoveInput(new Vector2(0, -1), run: false);
        await context.StepPhysics(40);

        var velocity = fixture.Player.Velocity;
        context.AssertTrue(velocity.X < -1.5f, $"yaw 90 forward should push -X (got {velocity.X:0.00})");
        context.AssertNear(0, velocity.Z, 0.4, "no Z drift at yaw 90");
    }
}

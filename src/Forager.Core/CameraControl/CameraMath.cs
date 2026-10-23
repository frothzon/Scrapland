namespace Forager.Core.CameraControl;

/// <summary>
/// Fixed-camera math (no engine types). The camera looks down at a constant pitch and never rotates
/// with the mouse, so movement is relative to its yaw: at yaw 0 "forward" is world −Z and "right" is +X.
/// </summary>
public static class CameraMath
{
    public const double DegreesToRadians = Math.PI / 180d;

    public readonly record struct PlanarBasis(double ForwardX, double ForwardZ, double RightX, double RightZ);

    public readonly record struct Offset(double Y, double Z);

    public static PlanarBasis BasisForYaw(double yawDegrees)
    {
        var yaw = yawDegrees * DegreesToRadians;
        return new PlanarBasis(
            ForwardX: -Math.Sin(yaw),
            ForwardZ: -Math.Cos(yaw),
            RightX: Math.Cos(yaw),
            RightZ: -Math.Sin(yaw));
    }

    /// <summary>
    /// Converts input to a world-space XZ direction. <paramref name="forward"/> is +1 for "towards the
    /// camera's forward", <paramref name="strafe"/> is +1 for "right".
    /// </summary>
    public static (double X, double Z) MoveDirection(double strafe, double forward, double yawDegrees)
    {
        var basis = BasisForYaw(yawDegrees);
        var x = basis.RightX * strafe + basis.ForwardX * forward;
        var z = basis.RightZ * strafe + basis.ForwardZ * forward;

        var length = Math.Sqrt((x * x) + (z * z));
        if (length < 1e-9)
        {
            return (0d, 0d);
        }

        if (length > 1d)
        {
            return (x / length, z / length);
        }

        return (x, z);
    }

    /// <summary>
    /// Offset from the focus point to the camera for a fixed pitch/distance: behind (+Z) and above (+Y).
    /// Pitch is negative when looking down (e.g. −55°).
    /// </summary>
    public static Offset OffsetFor(double pitchDegrees, double distance)
    {
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance cannot be negative.");
        }

        var pitch = pitchDegrees * DegreesToRadians;
        return new Offset(
            Y: distance * Math.Sin(-pitch),
            Z: distance * Math.Cos(pitch));
    }
}

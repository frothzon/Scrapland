using Forager.Core.CameraControl;

namespace Forager.Tests;

public sealed class CameraMathTests
{
    [Fact]
    public void BasisAtYawZero_LooksTowardNegativeZ()
    {
        var basis = CameraMath.BasisForYaw(0);

        Assert.Equal(0, basis.ForwardX, 9);
        Assert.Equal(-1, basis.ForwardZ, 9);
        Assert.Equal(1, basis.RightX, 9);
        Assert.Equal(0, basis.RightZ, 9);
    }

    [Fact]
    public void BasisAtYaw90_TurnsForwardTowardNegativeX()
    {
        var basis = CameraMath.BasisForYaw(90);

        Assert.Equal(-1, basis.ForwardX, 9);
        Assert.Equal(0, basis.ForwardZ, 9);
        Assert.Equal(0, basis.RightX, 9);
        Assert.Equal(-1, basis.RightZ, 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(90)]
    [InlineData(-120)]
    [InlineData(359)]
    public void Basis_IsUnitLengthAndOrthogonal(double yaw)
    {
        var basis = CameraMath.BasisForYaw(yaw);

        var forwardLength = Math.Sqrt((basis.ForwardX * basis.ForwardX) + (basis.ForwardZ * basis.ForwardZ));
        var rightLength = Math.Sqrt((basis.RightX * basis.RightX) + (basis.RightZ * basis.RightZ));
        var dot = (basis.ForwardX * basis.RightX) + (basis.ForwardZ * basis.RightZ);

        Assert.Equal(1, forwardLength, 9);
        Assert.Equal(1, rightLength, 9);
        Assert.Equal(0, dot, 9);
    }

    [Fact]
    public void MoveDirection_AtYawZero_ForwardIsNegativeZ()
    {
        var (x, z) = CameraMath.MoveDirection(strafe: 0, forward: 1, yawDegrees: 0);

        Assert.Equal(0, x, 9);
        Assert.Equal(-1, z, 9);
    }

    [Fact]
    public void MoveDirection_AtYaw90_ForwardIsNegativeX()
    {
        var (x, z) = CameraMath.MoveDirection(0, 1, 90);

        Assert.Equal(-1, x, 9);
        Assert.Equal(0, z, 9);
    }

    [Fact]
    public void MoveDirection_StrafeRightIsPositiveXAtYawZero()
    {
        var (x, z) = CameraMath.MoveDirection(1, 0, 0);

        Assert.Equal(1, x, 9);
        Assert.Equal(0, z, 9);
    }

    [Fact]
    public void MoveDirection_DiagonalIsNormalised()
    {
        var (x, z) = CameraMath.MoveDirection(1, 1, 0);

        var length = Math.Sqrt((x * x) + (z * z));
        Assert.Equal(1, length, 9);
    }

    [Fact]
    public void MoveDirection_ZeroInputStaysZero()
    {
        var (x, z) = CameraMath.MoveDirection(0, 0, 45);

        Assert.Equal(0, x);
        Assert.Equal(0, z);
    }

    [Fact]
    public void OffsetFor_PitchMinus55_Distance14_MatchesFixedCamera()
    {
        var offset = CameraMath.OffsetFor(pitchDegrees: -55, distance: 14);

        Assert.Equal(11.47, offset.Y, 2);
        Assert.Equal(8.03, offset.Z, 2);
    }

    [Fact]
    public void OffsetFor_RejectsNegativeDistance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CameraMath.OffsetFor(-55, -1));
    }
}

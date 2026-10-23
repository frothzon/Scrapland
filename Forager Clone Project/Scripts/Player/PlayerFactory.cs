using Forager.Core.Config;
using ForagerClone.Cameras;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Player;

/// <summary>Builds the player body in code so the scene file stays trivial (and scale stays data-driven).</summary>
public static class PlayerFactory
{
    public const int PlayerLayer = 1 << 1;
    public const int WorldMask = 1;

    public static PlayerController Create(
        BalanceConfig balance,
        GameSession session,
        CameraRig? rig = null,
        Vector3? position = null)
    {
        ArgumentNullException.ThrowIfNull(balance);
        ArgumentNullException.ThrowIfNull(session);

        var body = new PlayerController
        {
            Name = "Player",
            Tuning = balance.Player,
            Rig = rig,
            Session = session,
            CollisionLayer = PlayerLayer,
            CollisionMask = WorldMask,
            Position = position ?? Vector3.Zero,
        };

        body.AddChild(new CollisionShape3D
        {
            Name = "Collision",
            Shape = new CapsuleShape3D { Radius = 0.3f, Height = 1.4f },
            Position = new Vector3(0, 0.7f, 0),
        });

        body.AddChild(new PlayerAnimator
        {
            Name = "ModelAnchor",
            ModelScale = balance.Player.ModelScale,
        });

        return body;
    }
}

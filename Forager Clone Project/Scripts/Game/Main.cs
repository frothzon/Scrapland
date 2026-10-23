using ForagerClone.Cameras;
using ForagerClone.Interaction;
using ForagerClone.Nodes;
using ForagerClone.Player;
using ForagerClone.Save;
using ForagerClone.Ui;
using ForagerClone.World;
using Godot;

namespace ForagerClone.Game;

/// <summary>
/// Root scene. Builds the M1 slice in code (ground, camera rig, player, world nodes, interactor, HUD) so
/// nothing depends on hand-authored scene files and every step stays testable.
/// </summary>
public partial class Main : Node3D
{
    public GameSession Session { get; private set; } = null!;

    public PlayerController Player { get; private set; } = null!;

    public CameraRig Rig { get; private set; } = null!;

    public Interactor Interactor { get; private set; } = null!;

    public Hud HudLayer { get; private set; } = null!;

    public IReadOnlyList<ResourceNode3D> WorldNodes { get; private set; } = Array.Empty<ResourceNode3D>();

    public override void _Ready()
    {
        InputActions.EnsureRegistered();

        var seed = ParseSeedArgument();

        Session = new GameSession { Name = "GameSession" };
        if (seed is not null)
        {
            Session.Seed = seed.Value;
        }

        AddChild(Session);

        AddChild(SliceWorldBuilder.BuildGround(Session.Balance.World.PlotSize));

        Rig = new CameraRig { Name = "CameraRig" };
        AddChild(Rig);
        Rig.Configure(Session.Balance.Camera);

        Player = PlayerFactory.Create(Session.Balance, Session, rig: Rig);
        AddChild(Player);
        Session.Player = Player;
        Rig.Target = Player;
        Rig.SnapToTarget();

        var worldRoot = new Node3D { Name = "WorldNodes" };
        AddChild(worldRoot);
        WorldNodes = SliceWorldBuilder.Build(worldRoot, Session.Nodes, Session.Balance, Session, Session.ActiveSeed);
        Session.RegisterWorldNodes(WorldNodes);

        Interactor = new Interactor { Name = "Interactor", Session = Session, AimCamera = Rig.Camera };
        Player.AddChild(Interactor);
        Interactor.Configure(Session.Balance.Gather.RangeMeters);

        HudLayer = new Hud { Name = "Hud" };
        AddChild(HudLayer);
        HudLayer.Bind(Session, Interactor);

        Session.Persistence = new GodotPersistence();

        var version = Engine.GetVersionInfo().TryGetValue("string", out var value) ? value.AsString() : "unknown";
        GD.Print(
            $"[main] slice ready | engine={version} | display={DisplayServer.GetName()} | " +
            $"seed={Session.ActiveSeed} nodes={WorldNodes.Count} tool={Session.EquippedToolId ?? "none"}");
    }

    private static int? ParseSeedArgument()
    {
        const string prefix = "--seed=";
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(argument[prefix.Length..], out var seed))
            {
                return seed;
            }
        }

        return null;
    }
}


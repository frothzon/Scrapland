using ForagerClone.Cameras;
using ForagerClone.Game;
using ForagerClone.Interaction;
using ForagerClone.Nodes;
using ForagerClone.Player;
using ForagerClone.World;
using Godot;

namespace ForagerClone.Tests.Headless.Support;

/// <summary>
/// Builds a miniature game world for tests: session + ground + camera rig + player + interactor, and an
/// optional seeded node population. Everything is added under one root so freeing the test frees it all.
/// </summary>
public sealed class SliceFixture
{
    private readonly List<ResourceNode3D> _nodes = [];

    private SliceFixture(
        Node3D root,
        Node3D worldRoot,
        GameSession session,
        CameraRig rig,
        PlayerController player,
        Interactor interactor)
    {
        Root = root;
        WorldRoot = worldRoot;
        Session = session;
        Rig = rig;
        Player = player;
        Interactor = interactor;
    }

    public Node3D Root { get; }

    public Node3D WorldRoot { get; }

    public GameSession Session { get; }

    public CameraRig Rig { get; }

    public PlayerController Player { get; }

    public Interactor Interactor { get; }

    public IReadOnlyList<ResourceNode3D> Nodes => _nodes;

    public static SliceFixture Create(Node host, int seed = 1337, bool spawnWorld = false, double plotSize = 16)
    {
        var root = new Node3D { Name = "SliceRoot" };
        host.AddChild(root);

        // AdvanceClock stays off so a test owns the timeline (no wall clock anywhere).
        var session = new GameSession { Name = "GameSession", Seed = seed, AdvanceClock = false };
        root.AddChild(session);

        root.AddChild(SliceWorldBuilder.BuildGround(plotSize));

        var rig = new CameraRig { Name = "CameraRig" };
        root.AddChild(rig);
        rig.Configure(session.Balance.Camera);

        var player = PlayerFactory.Create(session.Balance, session, rig: rig);
        root.AddChild(player);
        session.Player = player;
        rig.Target = player;
        rig.SnapToTarget();

        var worldRoot = new Node3D { Name = "WorldNodes" };
        root.AddChild(worldRoot);

        var fixture = new SliceFixture(root, worldRoot, session, rig, player, null!);

        if (spawnWorld)
        {
            var built = SliceWorldBuilder.Build(worldRoot, session.Nodes, session.Balance, session, seed);
            fixture._nodes.AddRange(built);
            session.RegisterWorldNodes(fixture._nodes);
        }

        var interactor = new Interactor
        {
            Name = "Interactor",
            Session = session,
            AimCamera = rig.Camera,
        };
        player.AddChild(interactor);
        interactor.Configure(session.Balance.Gather.RangeMeters);

        return new SliceFixture(root, worldRoot, session, rig, player, interactor);
    }

    /// <summary>Creates one node of a known type at a known spot (deterministic alternative to the generator).</summary>
    public ResourceNode3D SpawnNode(string definitionId, Vector3 position)
    {
        var definition = Session.Nodes.Get(definitionId);
        var mesh = GD.Load<Mesh>(definition.MeshPath);
        if (mesh is null)
        {
            throw new InvalidOperationException($"Could not load mesh '{definition.MeshPath}' for node '{definitionId}'.");
        }

        var node = NodeFactory.Create(definition, mesh, Session, Session.Clock);
        node.Position = position;
        WorldRoot.AddChild(node);

        _nodes.Add(node);
        Session.RegisterWorldNodes(_nodes);
        return node;
    }
}

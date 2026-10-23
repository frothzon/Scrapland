using Forager.Core.Animation;
using Godot;

namespace ForagerClone.Player;

/// <summary>
/// Instantiates the chibi model and drives locomotion clips. Clip lookup goes through
/// <see cref="ClipNameResolver"/> because every asset format names its clips differently (LL-014).
/// </summary>
public partial class PlayerAnimator : Node3D
{
    public const string DefaultModelPath = "res://Assets/ChibiCharacters/glb emission/student.glb";

    [Export] public string ModelPath { get; set; } = DefaultModelPath;

    [Export] public double ModelScale { get; set; } = 0.73;

    [Export] public float ModelYawOffsetDegrees { get; set; }

    public Node3D? Model { get; private set; }

    public AnimationPlayer? Animation { get; private set; }

    public LogicalClip CurrentClip { get; private set; } = LogicalClip.Idle;

    public IReadOnlyList<string> AvailableClips => _available;

    public IReadOnlyDictionary<LogicalClip, string> ResolvedClips => _resolved;

    private readonly List<string> _available = [];
    private readonly Dictionary<LogicalClip, string> _resolved = [];

    public override void _Ready() => Build();

    /// <summary>Loads and attaches the model. Idempotent so tests can call it explicitly.</summary>
    public void Build()
    {
        if (Model is not null)
        {
            return;
        }

        var scene = GD.Load<PackedScene>(ModelPath);
        if (scene is null)
        {
            GD.PushError($"PlayerAnimator: cannot load model '{ModelPath}'.");
            return;
        }

        Model = scene.Instantiate<Node3D>();
        Model.Name = "Model";
        Model.Scale = Vector3.One * (float)ModelScale;
        Model.RotationDegrees = new Vector3(0, ModelYawOffsetDegrees, 0);
        AddChild(Model);

        Animation = FindAnimationPlayer(Model);
        if (Animation is null)
        {
            GD.PushWarning($"PlayerAnimator: no AnimationPlayer inside '{ModelPath}'.");
            return;
        }

        _available.Clear();
        foreach (var clip in Animation.GetAnimationList())
        {
            _available.Add(clip.ToString());
        }

        _resolved.Clear();
        foreach (var clip in Enum.GetValues<LogicalClip>())
        {
            var name = ClipNameResolver.Resolve(_available, clip);
            if (name is not null)
            {
                _resolved[clip] = name;
            }
        }

        Play(LogicalClip.Idle);
    }

    public bool HasClip(LogicalClip clip) => _resolved.ContainsKey(clip);

    public bool Play(LogicalClip clip)
    {
        if (Animation is null || !_resolved.TryGetValue(clip, out var name))
        {
            return false;
        }

        if (Animation.CurrentAnimation != name)
        {
            Animation.Play(name);
        }

        CurrentClip = clip;
        return true;
    }

    /// <summary>Picks idle / walk / run / jump from movement state.</summary>
    public void ApplyLocomotion(Vector3 moveDirection, bool running, bool onFloor)
    {
        if (!onFloor)
        {
            if (!Play(LogicalClip.Jump))
            {
                Play(LogicalClip.Idle);
            }

            return;
        }

        if (moveDirection.LengthSquared() < 0.0001f)
        {
            Play(LogicalClip.Idle);
            return;
        }

        if (!Play(running ? LogicalClip.Run : LogicalClip.Walk))
        {
            Play(LogicalClip.Idle);
        }
    }

    private static AnimationPlayer? FindAnimationPlayer(Node root)
    {
        if (root is AnimationPlayer direct)
        {
            return direct;
        }

        foreach (var child in root.FindChildren("*", "AnimationPlayer", true, false))
        {
            if (child is AnimationPlayer found)
            {
                return found;
            }
        }

        return null;
    }
}

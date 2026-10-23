using System.Text.Json;
using Forager.Core.Data;
using Forager.Core.Items;

namespace Forager.Core.Config;

public sealed record PlayerTuning(
    double WalkSpeed,
    double RunSpeed,
    double Acceleration,
    double JumpVelocity,
    double Gravity,
    double ModelScale);

public sealed record CameraTuning(
    double YawDegrees,
    double PitchDegrees,
    double Distance,
    double TargetHeight,
    double FollowSharpness);

public sealed record GatherTuning(
    double RangeMeters,
    double SwingSeconds,
    double MagnetRadius,
    double MagnetSpeed);

public sealed record ProgressionTuning(int BaseXp, double CurveExponent, int MaxLevel);

public sealed record WorldTuning(int Seed, int NodeCount, double PlotSize);

/// <summary>
/// All gameplay tunables, loaded from <c>Data/balance.json</c>. Godot scripts must read numbers from
/// here instead of hardcoding them (agents.md §2).
/// </summary>
public sealed class BalanceConfig
{
    public const string DefaultPath = "Data/balance.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private BalanceConfig(
        PlayerTuning player,
        CameraTuning camera,
        GatherTuning gather,
        ProgressionTuning progression,
        WorldTuning world,
        IReadOnlyList<string> startingItems)
    {
        Player = player;
        Camera = camera;
        Gather = gather;
        Progression = progression;
        World = world;
        StartingItems = startingItems;
    }

    public PlayerTuning Player { get; }

    public CameraTuning Camera { get; }

    public GatherTuning Gather { get; }

    public ProgressionTuning Progression { get; }

    public WorldTuning World { get; }

    public IReadOnlyList<string> StartingItems { get; }

    public static BalanceConfig Load(IDataSource source, string path = DefaultPath)
    {
        if (!source.Exists(path))
        {
            throw new FileNotFoundException($"Balance file '{path}' was not found.", path);
        }

        return FromJson(source.ReadText(path), path);
    }

    public static BalanceConfig FromJson(string json, string origin = "<memory>")
    {
        BalanceFileDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<BalanceFileDto>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Balance data '{origin}' is not valid JSON: {exception.Message}", exception);
        }

        if (dto is null)
        {
            throw new InvalidDataException($"Balance data '{origin}' is empty.");
        }

        return new BalanceConfig(
            dto.Player.ToTuning(),
            dto.Camera.ToTuning(),
            dto.Gather.ToTuning(),
            dto.Progression.ToTuning(),
            dto.World.ToTuning(),
            dto.StartingItems is null ? Array.Empty<string>() : dto.StartingItems.ToArray());
    }

    /// <summary>Semantic problems in the loaded data. Empty means healthy.</summary>
    public IReadOnlyList<string> Validate(ItemDatabase items)
    {
        var problems = new List<string>();

        if (Player.WalkSpeed <= 0) problems.Add("player.walkSpeed must be positive.");
        if (Player.RunSpeed < Player.WalkSpeed) problems.Add("player.runSpeed must be >= player.walkSpeed.");
        if (Player.Acceleration <= 0) problems.Add("player.acceleration must be positive.");
        if (Player.Gravity <= 0) problems.Add("player.gravity must be positive.");
        if (Player.ModelScale <= 0) problems.Add("player.modelScale must be positive.");

        if (Camera.Distance <= 0) problems.Add("camera.distance must be positive.");
        if (Camera.FollowSharpness <= 0) problems.Add("camera.followSharpness must be positive.");
        if (Camera.PitchDegrees >= 0) problems.Add("camera.pitchDegrees must be negative (looking down).");

        if (Gather.RangeMeters <= 0) problems.Add("gather.rangeMeters must be positive.");
        if (Gather.SwingSeconds <= 0) problems.Add("gather.swingSeconds must be positive.");
        if (Gather.MagnetRadius < 0) problems.Add("gather.magnetRadius cannot be negative.");
        if (Gather.MagnetSpeed <= 0) problems.Add("gather.magnetSpeed must be positive.");

        if (Progression.BaseXp <= 0) problems.Add("progression.baseXp must be positive.");
        if (Progression.CurveExponent <= 0) problems.Add("progression.curveExponent must be positive.");
        if (Progression.MaxLevel < 1) problems.Add("progression.maxLevel must be at least 1.");

        if (World.PlotSize <= 0) problems.Add("world.plotSize must be positive.");
        if (World.NodeCount < 0) problems.Add("world.nodeCount cannot be negative.");

        if (StartingItems.Count == 0)
        {
            problems.Add("startingItems must not be empty.");
        }

        foreach (var itemId in StartingItems)
        {
            if (!items.Contains(itemId))
            {
                problems.Add($"startingItems references unknown item '{itemId}'.");
            }
        }

        return problems;
    }
}

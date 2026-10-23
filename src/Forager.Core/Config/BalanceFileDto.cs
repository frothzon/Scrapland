using System.Text.Json.Serialization;

namespace Forager.Core.Config;

/// <summary>Wire format for <c>Data/balance.json</c>. Kept separate so the domain records stay clean.</summary>
internal sealed class BalanceFileDto
{
    [JsonPropertyName("player")]
    public PlayerDto Player { get; set; } = new();

    [JsonPropertyName("camera")]
    public CameraDto Camera { get; set; } = new();

    [JsonPropertyName("gather")]
    public GatherDto Gather { get; set; } = new();

    [JsonPropertyName("progression")]
    public ProgressionDto Progression { get; set; } = new();

    [JsonPropertyName("world")]
    public WorldDto World { get; set; } = new();

    [JsonPropertyName("startingItems")]
    public List<string>? StartingItems { get; set; }
}

internal sealed class PlayerDto
{
    [JsonPropertyName("walkSpeed")] public double WalkSpeed { get; set; } = 3.5;
    [JsonPropertyName("runSpeed")] public double RunSpeed { get; set; } = 6.0;
    [JsonPropertyName("acceleration")] public double Acceleration { get; set; } = 30;
    [JsonPropertyName("jumpVelocity")] public double JumpVelocity { get; set; } = 5.0;
    [JsonPropertyName("gravity")] public double Gravity { get; set; } = 22;
    [JsonPropertyName("modelScale")] public double ModelScale { get; set; } = 0.73;

    public PlayerTuning ToTuning() => new(WalkSpeed, RunSpeed, Acceleration, JumpVelocity, Gravity, ModelScale);
}

internal sealed class CameraDto
{
    [JsonPropertyName("yawDegrees")] public double YawDegrees { get; set; }
    [JsonPropertyName("pitchDegrees")] public double PitchDegrees { get; set; } = -55;
    [JsonPropertyName("distance")] public double Distance { get; set; } = 14;
    [JsonPropertyName("targetHeight")] public double TargetHeight { get; set; } = 1.0;
    [JsonPropertyName("followSharpness")] public double FollowSharpness { get; set; } = 12;

    public CameraTuning ToTuning() => new(YawDegrees, PitchDegrees, Distance, TargetHeight, FollowSharpness);
}

internal sealed class GatherDto
{
    [JsonPropertyName("rangeMeters")] public double RangeMeters { get; set; } = 12;
    [JsonPropertyName("swingSeconds")] public double SwingSeconds { get; set; } = 0.35;
    [JsonPropertyName("magnetRadius")] public double MagnetRadius { get; set; } = 3.0;
    [JsonPropertyName("magnetSpeed")] public double MagnetSpeed { get; set; } = 9;

    public GatherTuning ToTuning() => new(RangeMeters, SwingSeconds, MagnetRadius, MagnetSpeed);
}

internal sealed class ProgressionDto
{
    [JsonPropertyName("baseXp")] public int BaseXp { get; set; } = 20;
    [JsonPropertyName("curveExponent")] public double CurveExponent { get; set; } = 1.5;
    [JsonPropertyName("maxLevel")] public int MaxLevel { get; set; } = 30;

    public ProgressionTuning ToTuning() => new(BaseXp, CurveExponent, MaxLevel);
}

internal sealed class WorldDto
{
    [JsonPropertyName("seed")] public int Seed { get; set; } = 1337;
    [JsonPropertyName("nodeCount")] public int NodeCount { get; set; } = 14;
    [JsonPropertyName("plotSize")] public double PlotSize { get; set; } = 16;

    public WorldTuning ToTuning() => new(Seed, NodeCount, PlotSize);
}

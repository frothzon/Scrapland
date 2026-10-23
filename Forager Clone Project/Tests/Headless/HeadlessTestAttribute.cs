namespace ForagerClone.Tests.Headless;

/// <summary>
/// Marks a class as a headless integration test. The class must derive from
/// <see cref="HeadlessTest"/> and have a parameterless constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class HeadlessTestAttribute : Attribute
{
    /// <summary>Comma-separated tags used by <c>--filter</c>, e.g. "smoke,input".</summary>
    public string Tags { get; init; } = string.Empty;

    public string[] TagList => Tags
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

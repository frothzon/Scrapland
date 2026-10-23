namespace Forager.Core.Items;

/// <summary>
/// Resolves tool identity from item data and answers "can this tool do that job?". Pure functions so the
/// gating rules can be tested without an engine.
/// </summary>
public static class ToolRules
{
    public const string ToolTag = "tool";

    /// <summary>
    /// Kind + tier for an item, or null when the item is not a tool. Kind comes from <c>toolKind</c> or,
    /// when absent, the first non-"tool" tag (so existing `["tool","axe"]` items keep working).
    /// </summary>
    public static ToolSpec? Of(ItemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!definition.HasTag(ToolTag))
        {
            return null;
        }

        var kind = !string.IsNullOrWhiteSpace(definition.ToolKind)
            ? definition.ToolKind!
            : InferKind(definition);

        return string.IsNullOrWhiteSpace(kind)
            ? null
            : new ToolSpec(kind.Trim().ToLowerInvariant(), Math.Max(1, definition.ToolTier));
    }

    /// <summary>First tag that is not the generic <c>tool</c> tag (e.g. "axe", "pickaxe", "weapon").</summary>
    public static string? InferKind(ItemDefinition definition)
    {
        foreach (var tag in definition.Tags)
        {
            if (!string.Equals(tag, ToolTag, StringComparison.OrdinalIgnoreCase))
            {
                return tag;
            }
        }

        return null;
    }

    /// <summary>True when <paramref name="equipped"/> can do a job needing <paramref name="required"/>.</summary>
    public static bool Satisfies(ToolSpec? required, ToolSpec? equipped)
    {
        if (required is not { IsValid: true } needed)
        {
            return true;
        }

        return equipped is { IsValid: true } held
               && string.Equals(held.Kind, needed.Kind, StringComparison.OrdinalIgnoreCase)
               && held.Tier >= needed.Tier;
    }
}

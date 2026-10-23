namespace Forager.Core.Items;

/// <summary>
/// Immutable definition of an item type. Definitions come from <c>Data/items.json</c>;
/// gameplay code must never hardcode item ids (see agents.md).
/// </summary>
public sealed record ItemDefinition(
    string Id,
    string Name,
    int MaxStack,
    int Value,
    IReadOnlyList<string> Tags,
    string? Icon)
{
    public bool HasTag(string tag)
    {
        for (var i = 0; i < Tags.Count; i++)
        {
            if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

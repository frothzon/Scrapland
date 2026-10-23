using Forager.Core.Ui;
using Godot;

namespace ForagerClone.Ui;

/// <summary>Turns logical icon names into <see cref="AtlasTexture"/> regions of the Sprout Lands sheet.</summary>
public sealed class IconAtlas
{
    private readonly IconAtlasData _data;
    private readonly Dictionary<string, Texture2D?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IconAtlas(IconAtlasData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public IconAtlasData Data => _data;

    public Texture2D? SheetTexture { get; private set; }

    private bool _sheetLoaded;

    public Texture2D? Get(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        if (_cache.TryGetValue(iconName, out var cached))
        {
            return cached;
        }

        var texture = Build(iconName);
        _cache[iconName] = texture;
        return texture;
    }

    private AtlasTexture? Build(string iconName)
    {
        if (!_data.TryResolve(iconName, out var sheet, out var x, out var y))
        {
            return null;
        }

        if (!_sheetLoaded)
        {
            SheetTexture = GD.Load<Texture2D>(sheet.Path);
            _sheetLoaded = true;
        }

        if (SheetTexture is null)
        {
            GD.PushWarning($"IconAtlas: cannot load sheet '{sheet.Path}'.");
            return null;
        }

        return new AtlasTexture
        {
            Atlas = SheetTexture,
            Region = new Rect2(x, y, sheet.Cell, sheet.Cell),
        };
    }
}

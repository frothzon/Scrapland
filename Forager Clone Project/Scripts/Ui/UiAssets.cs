using Godot;

namespace ForagerClone.Ui;

/// <summary>
/// Single home for UI asset paths and the shared panel style, so panels contain no literals.
/// The vendor dialog-box nine-slice can replace <see cref="PanelStyle"/> in the M7 polish pass.
/// </summary>
public static class UiAssets
{
    public const string FontPath = "res://Assets/Sprout Lands - UI Pack - Basic pack/fonts/pixelFont-7-8x14-sproutLands.ttf";

    public const string IconSheetPath = "res://Assets/Sprout Lands - UI Pack - Basic pack/Sprite sheets/Icons/All Icons.png";

    public static readonly Color PanelFill = new(0.16f, 0.13f, 0.11f, 0.92f);
    public static readonly Color PanelBorder = new(0.85f, 0.72f, 0.45f, 1f);
    public static readonly Color TextColor = new(0.96f, 0.93f, 0.86f, 1f);
    public static readonly Color MutedColor = new(0.72f, 0.68f, 0.60f, 1f);
    public static readonly Color ReadyColor = new(0.62f, 0.86f, 0.52f, 1f);
    public static readonly Color LockedColor = new(0.86f, 0.55f, 0.48f, 1f);

    public static Theme CreateTheme()
    {
        var theme = new Theme { DefaultFontSize = 14 };
        if (GD.Load<FontFile>(FontPath) is { } font)
        {
            theme.DefaultFont = font;
        }

        return theme;
    }

    public static StyleBoxFlat CreatePanelStyle(Color? fill = null)
    {
        return new StyleBoxFlat
        {
            BgColor = fill ?? PanelFill,
            BorderColor = PanelBorder,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
    }

    public static PanelContainer CreatePanel(string name, Rect2 rect)
    {
        var panel = new PanelContainer
        {
            Name = name,
            Position = rect.Position,
            Size = rect.Size,
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
        return panel;
    }

    public static Label CreateLabel(string name, string text, int fontSize = 14, Color? color = null)
    {
        var label = new Label { Name = name, Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color ?? TextColor);
        return label;
    }
}

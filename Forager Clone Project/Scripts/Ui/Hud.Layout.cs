using Godot;

namespace ForagerClone.Ui;

public partial class Hud
{
    /// <summary>Constructs the whole HUD tree. Kept separate so <c>Hud.cs</c> stays about state binding.</summary>
    private void BuildUi()
    {
        if (IsBuilt)
        {
            return;
        }

        var font = GD.Load<FontFile>(FontPath);
        var theme = new Theme { DefaultFontSize = 14 };
        if (font is not null)
        {
            theme.DefaultFont = font;
        }

        var root = new Control
        {
            Name = "Root",
            Theme = theme,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(1280, 720),
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var status = new PanelContainer
        {
            Name = "Status",
            Position = new Vector2(12, 10),
            Size = new Vector2(220, 84),
        };

        var statusBox = new VBoxContainer { Name = "StatusBox" };
        status.AddChild(statusBox);

        _levelLabel = new Label { Name = "Level", Text = "Lv 1" };
        _xpBar = new ProgressBar
        {
            Name = "Xp",
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(200, 10),
        };
        _coinsLabel = new Label { Name = "Coins", Text = "Coins 0" };
        _toolLabel = new Label { Name = "Tool", Text = "Tool: hands" };

        statusBox.AddChild(_levelLabel);
        statusBox.AddChild(_xpBar);
        statusBox.AddChild(_coinsLabel);
        statusBox.AddChild(_toolLabel);
        root.AddChild(status);

        _messageLabel = new Label
        {
            Name = "Message",
            Text = string.Empty,
            Position = new Vector2(0, 120),
            Size = new Vector2(1280, 24),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        root.AddChild(_messageLabel);

        var hotbar = new HBoxContainer
        {
            Name = "Hotbar",
            Position = new Vector2(448, 630),
            Size = new Vector2(384, 72),
        };
        hotbar.AddThemeConstantOverride("separation", 6);
        root.AddChild(hotbar);

        for (var i = 0; i < HotbarSize; i++)
        {
            var slot = new PanelContainer
            {
                Name = $"Slot{i}",
                CustomMinimumSize = new Vector2(72, 72),
            };

            var box = new VBoxContainer();
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(64, 44),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            var count = new Label { Text = string.Empty, HorizontalAlignment = HorizontalAlignment.Center };

            box.AddChild(icon);
            box.AddChild(count);
            slot.AddChild(box);
            hotbar.AddChild(slot);

            _slots.Add(new SlotView(slot, icon, count));
        }

        IsBuilt = true;
    }
}

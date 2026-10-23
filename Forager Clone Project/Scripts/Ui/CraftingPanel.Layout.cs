using Godot;

namespace ForagerClone.Ui;

public partial class CraftingPanel
{
    /// <summary>Builds the panel tree once; rows are created here and reused by <see cref="Refresh"/>.</summary>
    private void BuildUi()
    {
        if (IsBuilt)
        {
            return;
        }

        var root = new Control
        {
            Name = "Root",
            Theme = UiAssets.CreateTheme(),
            Size = new Vector2(1280, 720),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var panel = UiAssets.CreatePanel("CraftPanel", new Rect2(300, 120, 680, 470));
        root.AddChild(panel);

        var column = new VBoxContainer { Name = "Column" };
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);

        _title = UiAssets.CreateLabel("Title", "Hand crafting", 18);
        column.AddChild(_title);

        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            CustomMinimumSize = new Vector2(640, 330),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        column.AddChild(scroll);

        var list = new VBoxContainer { Name = "Rows", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);
        _list = list;

        for (var i = 0; i < MaxRows; i++)
        {
            var rowPanel = UiAssets.CreatePanel($"Row{i}", new Rect2(0, 0, 640, 52));
            rowPanel.CustomMinimumSize = new Vector2(640, 52);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            rowPanel.AddChild(row);

            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(40, 40),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(icon);

            var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var name = UiAssets.CreateLabel("Name", string.Empty, 15);
            var inputs = UiAssets.CreateLabel("Inputs", string.Empty, 12, UiAssets.MutedColor);
            text.AddChild(name);
            text.AddChild(inputs);
            row.AddChild(text);

            var status = UiAssets.CreateLabel("Status", string.Empty, 12, UiAssets.MutedColor);
            status.CustomMinimumSize = new Vector2(120, 0);
            status.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(status);

            var craftIndex = i;
            var craft = new Button { Name = $"Craft{i}", Text = "Craft", CustomMinimumSize = new Vector2(110, 36) };
            craft.Pressed += () => TryCraft(craftIndex);
            row.AddChild(craft);

            list.AddChild(rowPanel);
            _rows.Add(new RowView(rowPanel, icon, name, inputs, status, craft));
        }

        _hint = UiAssets.CreateLabel("Hint", string.Empty, 12, UiAssets.MutedColor);
        column.AddChild(_hint);

        IsBuilt = true;
    }
}

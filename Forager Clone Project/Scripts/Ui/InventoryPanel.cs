using Forager.Core.Items;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Ui;

/// <summary>
/// 6x4 inventory grid overlay (Tab). Reads the Core inventory through <c>GameSession.Profile</c> and
/// renders icons from the shared atlas; opening it suspends player input via <see cref="GameSession.UiOpen"/>.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    public const int Columns = 6;
    public const int Rows = 4;
    public const int SlotCount = Columns * Rows;

    private readonly List<SlotView> _slots = [];
    private GameSession? _session;
    private ItemDatabase? _items;
    private IconAtlas? _atlas;
    private Label _title = null!;

    public bool IsOpen => Visible;

    public int VisibleSlotCount => _slots.Count;

    public override void _Ready()
    {
        BuildUi();
        Visible = false;
    }

    public void Bind(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_session is not null)
        {
            _session.Profile.Inventory.Changed -= Refresh;
        }

        _session = session;
        _items = session.Items;
        _atlas = new IconAtlas(session.Icons);
        _session.Profile.Inventory.Changed += Refresh;
        Refresh();
    }

    public void Open()
    {
        if (_session is null)
        {
            return;
        }

        _session.OpenInventory();
        Visible = true;
        Refresh();
    }

    public void Close()
    {
        Visible = false;
        _session?.CloseUi();
    }

    public void Toggle()
    {
        if (Visible)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public string GetSlotText(int index) => index >= 0 && index < _slots.Count ? _slots[index].Count.Text : string.Empty;

    public Texture2D? GetSlotIcon(int index) => index >= 0 && index < _slots.Count ? _slots[index].Icon.Texture : null;

    public void Refresh()
    {
        if (_session is null || _items is null || _atlas is null)
        {
            return;
        }

        _title.Text = $"Inventory ({_session.Profile.Inventory.SlotCount} slots)";

        for (var i = 0; i < _slots.Count; i++)
        {
            if (i >= _session.Profile.Inventory.SlotCount)
            {
                _slots[i].Root.Visible = false;
                continue;
            }

            _slots[i].Root.Visible = true;
            var stack = _session.Profile.Inventory.GetSlot(i);
            _slots[i].Count.Text = stack.IsEmpty ? string.Empty : stack.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _slots[i].Icon.Texture = stack.IsEmpty || !_items.TryGet(stack.ItemId, out var definition)
                ? null
                : _atlas.Get(definition.Icon);
        }
    }

    private void BuildUi()
    {
        var root = new Control
        {
            Name = "Root",
            Theme = UiAssets.CreateTheme(),
            Size = new Vector2(1280, 720),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var panel = UiAssets.CreatePanel("InventoryPanel", new Rect2(340, 150, 460, 380));
        root.AddChild(panel);

        var column = new VBoxContainer { Name = "Column" };
        panel.AddChild(column);

        _title = UiAssets.CreateLabel("Title", "Inventory", 16);
        column.AddChild(_title);

        var grid = new GridContainer { Name = "Grid", Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        column.AddChild(grid);

        for (var i = 0; i < SlotCount; i++)
        {
            var slot = new PanelContainer { Name = $"Slot{i}", CustomMinimumSize = new Vector2(68, 68) };
            slot.AddThemeStyleboxOverride("panel", UiAssets.CreatePanelStyle(new Color(0.22f, 0.19f, 0.16f, 0.95f)));

            var box = new VBoxContainer();
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(48, 40),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            var count = new Label { Text = string.Empty, HorizontalAlignment = HorizontalAlignment.Center };

            box.AddChild(icon);
            box.AddChild(count);
            slot.AddChild(box);
            grid.AddChild(slot);

            _slots.Add(new SlotView(slot, icon, count));
        }
    }

    private readonly record struct SlotView(PanelContainer Root, TextureRect Icon, Label Count);
}

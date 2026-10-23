using Forager.Core.Items;
using Forager.Core.Player;
using ForagerClone.Game;
using ForagerClone.Interaction;
using Godot;

namespace ForagerClone.Ui;

/// <summary>
/// Minimal M1 HUD: hotbar (first inventory slots), XP bar, level, coins and a feedback banner. Built in
/// code so there is no fragile hand-authored scene, and so tests can read the labels/icon textures.
/// </summary>
public partial class Hud : CanvasLayer
{
    public const string FontPath = "res://Assets/Sprout Lands - UI Pack - Basic pack/fonts/pixelFont-7-8x14-sproutLands.ttf";

    public const int HotbarSize = 5;

    private readonly List<SlotView> _slots = [];

    private Label _levelLabel = null!;
    private Label _coinsLabel = null!;
    private Label _messageLabel = null!;
    private ProgressBar _xpBar = null!;

    private PlayerProfile? _profile;
    private ItemDatabase? _items;
    private IconAtlas? _atlas;

    public Interactor? Interactor { get; set; }

    public bool IsBuilt { get; private set; }

    public int SlotCount => _slots.Count;

    public string MessageText => _messageLabel.Text;

    public string LevelText => _levelLabel.Text;

    public string CoinsText => _coinsLabel.Text;

    public double XpValue => _xpBar.Value;

    public override void _Ready()
    {
        BuildUi();
        Refresh();
    }

    public void Bind(GameSession session, Interactor? interactor = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_profile is not null)
        {
            _profile.Inventory.Changed -= Refresh;
            _profile.Changed -= Refresh;
        }

        _profile = session.Profile;
        _items = session.Items;
        _atlas = new IconAtlas(session.Icons);
        Interactor = interactor;

        _profile.Inventory.Changed += Refresh;
        _profile.Changed += Refresh;

        Refresh();
    }

    public string GetSlotText(int index) => index >= 0 && index < _slots.Count ? _slots[index].Count.Text : string.Empty;

    public Texture2D? GetSlotIcon(int index) => index >= 0 && index < _slots.Count ? _slots[index].Icon.Texture : null;

    public override void _Process(double delta)
    {
        if (Interactor is not null && _messageLabel.Text != Interactor.Message)
        {
            _messageLabel.Text = Interactor.Message;
        }
    }

    private void Refresh()
    {
        if (!IsBuilt || _profile is null || _items is null || _atlas is null)
        {
            return;
        }

        _levelLabel.Text = $"Lv {_profile.Level}";
        _coinsLabel.Text = $"Coins {_profile.Coins}";
        _xpBar.MaxValue = 1;
        _xpBar.Value = _profile.LevelProgress;

        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var stack = _profile.Inventory.GetSlot(i);

            if (stack.IsEmpty)
            {
                slot.Count.Text = string.Empty;
                slot.Icon.Texture = null;
                slot.Root.Modulate = new Color(1, 1, 1, 0.55f);
                continue;
            }

            slot.Count.Text = stack.Amount.ToString();
            slot.Icon.Texture = _items.TryGet(stack.ItemId, out var definition) ? _atlas.Get(definition.Icon) : null;
            slot.Root.Modulate = Colors.White;
        }
    }

    private readonly record struct SlotView(PanelContainer Root, TextureRect Icon, Label Count);
}

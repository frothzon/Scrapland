using System.Globalization;
using Forager.Core.Crafting;
using Forager.Core.Items;
using ForagerClone.Game;
using Godot;

namespace ForagerClone.Ui;

/// <summary>
/// Station craft panel (C key, or E on a station). Lists the recipes available at the active station with
/// have/need counts, lock reasons and a craft button. Rules come from <c>CraftingService</c>.
/// </summary>
public partial class CraftingPanel : CanvasLayer
{
    public const int MaxRows = 12;

    private readonly List<RowView> _rows = [];
    private readonly List<RecipeDefinition> _visible = [];

    private GameSession? _session;
    private ItemDatabase? _items;
    private IconAtlas? _atlas;

    private Label _title = null!;
    private Label _hint = null!;
    private VBoxContainer _list = null!;

    public bool IsOpen => Visible;

    public int RecipeCount => _visible.Count;

    public bool IsBuilt { get; private set; }

    public string TitleText => _title.Text;

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
            _session.Profile.Changed -= Refresh;
        }

        _session = session;
        _items = session.Items;
        _atlas = new IconAtlas(session.Icons);
        _session.Profile.Inventory.Changed += Refresh;
        _session.Profile.Changed += Refresh;
        Refresh();
    }

    public void Open()
    {
        if (_session is null)
        {
            return;
        }

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

    public string GetRecipeId(int index) => index >= 0 && index < _visible.Count ? _visible[index].Id : string.Empty;

    public string GetRowStatus(int index) => index >= 0 && index < _rows.Count ? _rows[index].Status.Text : string.Empty;

    public string GetRowInputs(int index) => index >= 0 && index < _rows.Count ? _rows[index].Inputs.Text : string.Empty;

    public bool IsRowEnabled(int index) => index >= 0 && index < _rows.Count && !_rows[index].Craft.Disabled;

    public Texture2D? GetRowIcon(int index) => index >= 0 && index < _rows.Count ? _rows[index].Icon.Texture : null;

    /// <summary>Crafts the recipe shown in row <paramref name="index"/> (test/UI seam).</summary>
    public CraftOutcome? TryCraft(int index)
    {
        if (_session is null || index < 0 || index >= _visible.Count)
        {
            return null;
        }

        var outcome = _session.Craft(_visible[index].Id);
        Refresh();
        return outcome;
    }

    public void Refresh()
    {
        if (!IsBuilt || _session is null || _items is null || _atlas is null)
        {
            return;
        }

        _visible.Clear();
        _visible.AddRange(_session.VisibleRecipes());

        _title.Text = _session.ActiveStation is { } station ? $"{station} crafting" : "Hand crafting";
        _hint.Text = _session.ActiveStation is null
            ? "C: hand recipes    Tab: inventory    Esc: close"
            : "C: recipes    Tab: inventory    Esc: close";

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];

            if (i >= _visible.Count)
            {
                row.Root.Visible = false;
                continue;
            }

            var recipe = _visible[i];
            var lockReason = _session.LockReason(recipe);
            var canCraft = _session.CanCraft(recipe);

            row.Root.Visible = true;
            row.Name.Text = DescribeOutputs(recipe);
            row.Inputs.Text = DescribeInputs(recipe);
            row.Inputs.AddThemeColorOverride("font_color", canCraft ? UiAssets.TextColor : UiAssets.MutedColor);
            row.Icon.Texture = IconFor(recipe.Outputs[0].ItemId);
            row.Status.Text = lockReason ?? (canCraft ? "Ready" : "Missing");
            row.Status.AddThemeColorOverride("font_color", lockReason is null && canCraft ? UiAssets.ReadyColor : UiAssets.LockedColor);
            row.Craft.Text = lockReason ?? "Craft";
            row.Craft.Disabled = lockReason is not null || !canCraft;
        }
    }

    private string DescribeOutputs(RecipeDefinition recipe)
        => string.Join(", ", recipe.Outputs.Select(output => $"{ItemName(output.ItemId)} x{output.Amount}"));

    private string DescribeInputs(RecipeDefinition recipe)
        => string.Join(
            "   ",
            recipe.Inputs.Select(input =>
                $"{ItemName(input.ItemId)} {_session!.Profile.Inventory.Count(input.ItemId).ToString(CultureInfo.InvariantCulture)}/{input.Amount.ToString(CultureInfo.InvariantCulture)}"));

    private string ItemName(string itemId) => _items!.TryGet(itemId, out var definition) ? definition.Name : itemId;

    private Texture2D? IconFor(string itemId)
        => _items!.TryGet(itemId, out var definition) ? _atlas!.Get(definition.Icon) : null;

    private readonly record struct RowView(
        PanelContainer Root,
        TextureRect Icon,
        Label Name,
        Label Inputs,
        Label Status,
        Button Craft);
}

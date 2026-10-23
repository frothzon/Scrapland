using Forager.Core.Config;
using Forager.Core.Gathering;
using Forager.Core.Inventories;
using Forager.Core.Items;
using Forager.Core.Nodes;
using Forager.Core.Player;
using Forager.Core.Randomness;
using Forager.Core.Save;
using Forager.Core.Sim;
using Forager.Core.Ui;
using ForagerClone.Nodes;
using Godot;

namespace ForagerClone.Game;

/// <summary>
/// Owns the live session state (databases, clock, profile, RNG) and is the only place Godot code touches
/// Core rules. Everything gameplay-relevant comes from <c>Data/*.json</c>.
/// </summary>
public partial class GameSession : Node
{
    [Export] public bool AdvanceClock { get; set; } = true;

    /// <summary>0 means "use the seed from Data/balance.json".</summary>
    [Export] public int Seed { get; set; }

    [Export] public bool AutoSave { get; set; }

    public SimClock Clock { get; private set; } = new();

    public ItemDatabase Items { get; private set; } = null!;

    public NodeDatabase Nodes { get; private set; } = null!;

    public BalanceConfig Balance { get; private set; } = null!;

    public IconAtlasData Icons { get; private set; } = null!;

    public SeededRng Rng { get; private set; } = null!;

    public PlayerProfile Profile { get; private set; } = null!;

    public HarvestService Harvest { get; private set; } = null!;

    public IPersistence? Persistence { get; set; }

    public Node3D? Player { get; set; }

    public IReadOnlyList<ResourceNode3D> WorldNodes { get; private set; } = Array.Empty<ResourceNode3D>();

    public string StatusMessage { get; private set; } = string.Empty;

    public bool IsBooted => Profile is not null;

    public int ActiveSeed { get; private set; }

    private double _lastAutoSaveSeconds;

    public override void _Ready()
    {
        // Idempotent: guarantees input actions exist even when a test creates a session without Main.
        InputActions.EnsureRegistered();

        if (!IsBooted)
        {
            Boot(Seed == 0 ? null : Seed);
        }
    }

    /// <summary>Loads all data and creates the profile. Safe to call once.</summary>
    public void Boot(int? seedOverride = null)
    {
        var data = new GodotDataSource();
        Items = ItemDatabase.Load(data);
        Nodes = NodeDatabase.Load(data);
        Balance = BalanceConfig.Load(data);
        Icons = IconAtlasData.Load(data);

        ActiveSeed = seedOverride ?? Balance.World.Seed;
        Rng = new SeededRng(ActiveSeed);
        Clock = new SimClock();
        Profile = new PlayerProfile(
            new Inventory(24, Items),
            new ExperienceCurve(Balance.Progression.BaseXp, Balance.Progression.CurveExponent, Balance.Progression.MaxLevel));
        Harvest = new HarvestService(Balance.Gather, Rng);

        foreach (var itemId in Balance.StartingItems)
        {
            Profile.Inventory.Add(itemId, 1);
        }

        _lastAutoSaveSeconds = 0;
    }

    public void RegisterWorldNodes(IReadOnlyList<ResourceNode3D> nodes) => WorldNodes = nodes;

    /// <summary>The first tool in the inventory (M1 has no hotbar selection yet).</summary>
    public string? EquippedToolId
    {
        get
        {
            if (!IsBooted)
            {
                return null;
            }

            for (var i = 0; i < Profile.Inventory.SlotCount; i++)
            {
                var slot = Profile.Inventory.GetSlot(i);
                if (slot.IsEmpty)
                {
                    continue;
                }

                if (Items.TryGet(slot.ItemId, out var definition) && definition.HasTag("tool"))
                {
                    return definition.Id;
                }
            }

            return null;
        }
    }

    public double NowSeconds => Clock.TotalSeconds;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsBooted)
        {
            return;
        }

        if (AdvanceClock)
        {
            Clock.Advance(delta);
        }

        if (AutoSave && Persistence is not null && Clock.TotalSeconds - _lastAutoSaveSeconds >= 60)
        {
            _lastAutoSaveSeconds = Clock.TotalSeconds;
            SaveNow();
        }
    }
}

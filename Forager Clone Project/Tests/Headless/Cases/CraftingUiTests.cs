using Forager.Core.Crafting;
using ForagerClone.Tests.Headless.Support;
using ForagerClone.Ui;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>The craft panel lists station recipes, shows have/need, and crafts through the Core service.</summary>
[HeadlessTest(Tags = "crafting,ui")]
public partial class CraftingPanelTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        fixture.SpawnStation(StationKind.Workbench, new Vector3(0, 0, -2f));
        fixture.Player.GlobalPosition = new Vector3(0, 0, -0.5f);
        fixture.GiveItems(("wood", 6));
        await context.StepPhysics(1);

        var panel = new CraftingPanel { Name = "TestCraftPanel" };
        AddChild(panel);
        await context.StepPhysics(1);
        panel.Bind(fixture.Session);

        context.AssertTrue(panel.IsBuilt, "the panel built its tree");

        fixture.Session.OpenCrafting(StationKind.Workbench);
        panel.Open();
        await context.StepPhysics(1);

        context.AssertTrue(panel.IsOpen, "the panel is visible");
        context.AssertTrue(panel.RecipeCount >= 5, $"the station lists its recipes ({panel.RecipeCount})");

        var plankIndex = Find(panel, "plank");
        context.AssertTrue(plankIndex >= 0, "the plank recipe is listed");
        context.AssertTrue(panel.IsRowEnabled(plankIndex), "plank is craftable with wood");
        context.AssertTrue(
            panel.GetRowInputs(plankIndex).Contains("/2", StringComparison.Ordinal),
            $"have/need is shown (got '{panel.GetRowInputs(plankIndex)}')");

        var outcome = panel.TryCraft(plankIndex);
        context.AssertNotNull(outcome, "craft ran");
        context.AssertTrue(outcome!.Success, $"craft succeeded ({outcome.Status})");
        context.AssertEqual(1, fixture.Session.Profile.Inventory.Count("plank"), "a plank landed in the inventory");
        context.AssertEqual(4, fixture.Session.Profile.Inventory.Count("wood"), "two wood were consumed");
        context.AssertTrue(fixture.Session.Profile.TotalXp > 0, "crafting awarded xp");

        var lockedIndex = Find(panel, "stone_axe");
        context.AssertTrue(lockedIndex >= 0, "the level-locked recipe is listed");
        context.AssertFalse(panel.IsRowEnabled(lockedIndex), "a level-2 recipe is disabled at level 1");
        context.AssertTrue(
            panel.GetRowStatus(lockedIndex).StartsWith("Lv", StringComparison.Ordinal),
            $"the status shows the required level (got '{panel.GetRowStatus(lockedIndex)}')");
    }

    private static int Find(CraftingPanel panel, string recipeId)
    {
        for (var i = 0; i < panel.RecipeCount; i++)
        {
            if (string.Equals(panel.GetRecipeId(i), recipeId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>The inventory panel shows every slot and owns the UI flag.</summary>
[HeadlessTest(Tags = "ui,inventory")]
public partial class InventoryPanelTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        fixture.GiveItems(("wood", 12), ("stone", 3));
        await context.StepPhysics(1);

        var panel = new InventoryPanel { Name = "TestInventoryPanel" };
        AddChild(panel);
        await context.StepPhysics(1);
        panel.Bind(fixture.Session);

        context.AssertEqual(InventoryPanel.SlotCount, panel.VisibleSlotCount, "24 slots are built");
        context.AssertFalse(panel.IsOpen, "the panel starts closed");

        panel.Open();
        await context.StepPhysics(1);

        context.AssertTrue(panel.IsOpen, "Tab opens the panel");
        context.AssertTrue(fixture.Session.UiOpen, "opening sets the session UI flag");
        context.AssertEqual("1", panel.GetSlotText(0), "the axe occupies slot 0");
        context.AssertNotNull(panel.GetSlotIcon(0), "slot icons resolve from the atlas");

        panel.Close();
        context.AssertFalse(panel.IsOpen, "closing hides the panel");
        context.AssertFalse(fixture.Session.UiOpen, "closing clears the UI flag");
    }
}

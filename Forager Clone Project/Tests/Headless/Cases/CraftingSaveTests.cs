using Forager.Core.Crafting;
using Forager.Core.Save;
using ForagerClone.Tests.Headless.Support;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Crafted state (recipes seen) must survive a save/load cycle — it drives <c>after:</c> unlocks.</summary>
[HeadlessTest(Tags = "crafting,save")]
public partial class CraftingSaveTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var fixture = SliceFixture.Create(this);
        var session = fixture.Session;
        fixture.GiveItems(("wood", 6));
        session.Persistence = new InMemoryPersistence();
        await context.StepPhysics(1);

        session.OpenCrafting(null);
        var crafted = session.Craft("plank");
        session.CloseUi();

        context.AssertTrue(crafted.Success, $"hand crafting succeeded ({crafted.Status})");
        context.AssertTrue(session.Book.HasCrafted("plank"), "the book recorded the craft");
        context.AssertEqual(1, session.Profile.Inventory.Count("plank"), "a plank exists");

        context.AssertTrue(session.SaveNow(), "save written");

        session.Profile.Inventory.Remove("plank", 1);
        session.Book.RestoreFrom(null);
        context.AssertFalse(session.Book.HasCrafted("plank"), "crafted state cleared before loading");

        context.AssertTrue(session.LoadNow(), "load succeeded");

        context.AssertEqual(1, session.Profile.Inventory.Count("plank"), "the plank was restored");
        context.AssertTrue(session.Book.HasCrafted("plank"), "the crafted set was restored");
    }
}

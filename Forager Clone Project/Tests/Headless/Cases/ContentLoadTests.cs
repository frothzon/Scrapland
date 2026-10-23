using Forager.Core.Items;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>
/// Content must load through <c>res://</c>, not just from disk: this catches missing files,
/// broken import settings and scenes that reference deleted resources.
/// </summary>
[HeadlessTest(Tags = "content")]
public partial class ContentLoadTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var json = Godot.FileAccess.GetFileAsString("res://Data/items.json");
        context.AssertFalse(string.IsNullOrWhiteSpace(json), "res://Data/items.json is readable through the engine");

        if (!string.IsNullOrWhiteSpace(json))
        {
            var items = ItemDatabase.FromJson(json, "res://Data/items.json");
            context.AssertTrue(items.Contains("wood"), "items.json contains the wood item");
            context.AssertTrue(items.Validate().Count == 0, "items.json passes validation");
        }

        var packed = ResourceLoader.Load<PackedScene>("res://Scenes/Main.tscn");
        context.AssertNotNull(packed, "res://Scenes/Main.tscn loads");

        if (packed is not null)
        {
            var main = packed.Instantiate();
            AddChild(main);
            await context.StepPhysics(1);

            var meshes = main.FindChildren("*", "MeshInstance3D", true, false);
            context.AssertTrue(meshes.Count > 0, "Main scene contains at least one MeshInstance3D");
            context.AssertNotNull(main.FindChild("Camera3D", true, false), "Main scene has a Camera3D");

            main.QueueFree();
        }
    }
}

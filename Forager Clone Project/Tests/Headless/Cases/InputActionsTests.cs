using ForagerClone.Game;
using Godot;

namespace ForagerClone.Tests.Headless.Cases;

/// <summary>Input actions are registered in code, so assert they really exist after boot.</summary>
[HeadlessTest(Tags = "smoke,input")]
public partial class InputActionsTests : HeadlessTest
{
    public override Task RunAsync(HeadlessTestContext context)
    {
        InputActions.EnsureRegistered();
        InputActions.EnsureRegistered(); // must be idempotent

        foreach (var action in InputActions.All)
        {
            context.AssertTrue(InputActions.IsRegistered(action), $"input action '{action}' is registered");
        }

        var useEvents = InputMap.ActionGetEvents(InputActions.Use);
        context.AssertTrue(useEvents.Count >= 2, "use_tool has both keyboard and mouse bindings");

        var moveEvents = InputMap.ActionGetEvents(InputActions.MoveForward);
        context.AssertEqual(1, moveEvents.Count, "move_forward registered exactly once after double registration");

        return Task.CompletedTask;
    }
}

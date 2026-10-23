using Godot;

namespace ForagerClone.Game;

/// <summary>
/// Registers the game's input actions in code.
/// Rationale: project.godot input serialization is fragile to hand-edit and invisible to tests;
/// registering here keeps the bindings reviewable and lets a headless test assert they exist
/// (see lessons_learned.md LL-010).
/// </summary>
public static class InputActions
{
    public const string MoveForward = "move_forward";
    public const string MoveBack = "move_back";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Run = "run";
    public const string Jump = "jump";
    public const string Interact = "interact";
    public const string Use = "use_tool";
    public const string Inventory = "inventory";
    public const string Craft = "craft";
    public const string Cancel = "cancel";
    public const string QuickSave = "quick_save";
    public const string QuickLoad = "quick_load";

    private static readonly (string Action, Key Key)[] KeyboardBindings =
    [
        (MoveForward, Key.W),
        (MoveBack, Key.S),
        (MoveLeft, Key.A),
        (MoveRight, Key.D),
        (Run, Key.Shift),
        (Jump, Key.Space),
        (Interact, Key.E),
        (Use, Key.F),
        (Inventory, Key.Tab),
        (Craft, Key.C),
        (Cancel, Key.Escape),
        (QuickSave, Key.F5),
        (QuickLoad, Key.F9),
    ];

    private static readonly (string Action, MouseButton Button)[] MouseBindings =
    [
        (Use, MouseButton.Left),
    ];

    public static IReadOnlyList<string> All { get; } =
        KeyboardBindings.Select(binding => binding.Action).Distinct().ToArray();

    /// <summary>Idempotent: safe to call from any scene or test.</summary>
    public static void EnsureRegistered()
    {
        foreach (var (action, key) in KeyboardBindings)
        {
            EnsureAction(action);
            if (!HasKeyboardBinding(action, key))
            {
                InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
            }
        }

        foreach (var (action, button) in MouseBindings)
        {
            EnsureAction(action);
            if (!HasMouseBinding(action, button))
            {
                InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = button });
            }
        }
    }

    public static bool IsRegistered(string action) => InputMap.HasAction(action);

    private static void EnsureAction(string action)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }
    }

    private static bool HasKeyboardBinding(string action, Key key)
    {
        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventKey keyEvent && keyEvent.PhysicalKeycode == key)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMouseBinding(string action, MouseButton button)
    {
        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == button)
            {
                return true;
            }
        }

        return false;
    }
}

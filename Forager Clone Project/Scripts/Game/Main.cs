using Godot;

namespace ForagerClone.Game;

/// <summary>Root scene for the game. Wires up global services and reports boot state.</summary>
public partial class Main : Node3D
{
    public override void _Ready()
    {
        InputActions.EnsureRegistered();

        var version = Engine.GetVersionInfo().TryGetValue("string", out var value) ? value.AsString() : "unknown";
        GD.Print($"[main] Forager Clone ready | engine={version} | display={DisplayServer.GetName()}");
    }
}

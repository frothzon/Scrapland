using Forager.Core.Save;
using Godot;

namespace ForagerClone.Game;

public partial class GameSession
{
    /// <summary>Writes the current session (profile, clock, world nodes, player position).</summary>
    public bool SaveNow()
    {
        if (!IsBooted || Persistence is null)
        {
            return false;
        }

        var save = SaveSerializer.Capture(Profile, Clock, ActiveSeed);

        if (Player is not null)
        {
            save.PlayerX = Player.GlobalPosition.X;
            save.PlayerY = Player.GlobalPosition.Y;
            save.PlayerZ = Player.GlobalPosition.Z;
        }

        for (var i = 0; i < WorldNodes.Count; i++)
        {
            var node = WorldNodes[i];
            save.Nodes.Add(new SavedNode
            {
                NodeId = node.Definition.Id,
                Index = i,
                HitPointsLeft = node.State.HitPointsLeft,
                RespawnAtSeconds = node.State.RespawnAtSeconds,
            });
        }

        Persistence.Save(SaveSerializer.ToJson(save));
        StatusMessage = "Saved";
        return true;
    }

    /// <summary>Loads the stored session over the live one. Returns false when nothing usable exists.</summary>
    public bool LoadNow()
    {
        if (!IsBooted || Persistence is null || !Persistence.TryLoad(out var json))
        {
            StatusMessage = "No save";
            return false;
        }

        SaveGame save;
        try
        {
            save = SaveSerializer.FromJson(json);
        }
        catch (InvalidDataException exception)
        {
            StatusMessage = "Save incompatible";
            GD.PushWarning(exception.Message);
            return false;
        }

        SaveSerializer.Restore(save, Profile, Clock);
        _lastAutoSaveSeconds = Clock.TotalSeconds;

        if (Player is not null)
        {
            Player.GlobalPosition = new Vector3((float)save.PlayerX, (float)save.PlayerY, (float)save.PlayerZ);
        }

        foreach (var saved in save.Nodes)
        {
            if (saved.Index >= 0 && saved.Index < WorldNodes.Count)
            {
                WorldNodes[saved.Index].RestoreState(saved.HitPointsLeft, saved.RespawnAtSeconds);
            }
        }

        StatusMessage = "Loaded";
        return true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.QuickSave))
        {
            SaveNow();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed(InputActions.QuickLoad))
        {
            LoadNow();
            GetViewport().SetInputAsHandled();
        }
    }
}

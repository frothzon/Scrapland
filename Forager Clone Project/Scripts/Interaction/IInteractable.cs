using Forager.Core.Gathering;
using Forager.Core.Nodes;
using Godot;

namespace ForagerClone.Interaction;

/// <summary>
/// Anything the aim ray can hit and harvest. Keeps <see cref="Interaction.Interactor"/> free of node
/// internals and lets tests drive harvesting without a mouse.
/// </summary>
public interface IInteractable
{
    Node3D Anchor { get; }

    NodeDefinition Definition { get; }

    ResourceNodeState State { get; }

    void ShowHitFeedback(bool felled);

    void SpawnDrops(IReadOnlyList<HarvestDrop> drops);
}

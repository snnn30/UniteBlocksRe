using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes.OperationItem;

public record class OperationState(
    BlockEntity Parent,
    BlockEntity Child,
    Vector2I ParentPos,
    Vector2I ChildPos,
    bool IsBetweenCells,
    OperationPhase Phase
);

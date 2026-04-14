using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class SpawnHandler
{
    public static OperationResult Spawn(
        OperationContext context,
        BlockEntity parent,
        BlockEntity child = null
    )
    {
        var current = context.OperationState;

        if (!context.CanOperate(OperationPhase.WaitingSpawn, current))
        {
            return OperationResult.Failed(current);
        }

        var parentPos = BoardEntity.SpawnPosition;
        var childPos = child is not null ? parentPos + Vector2I.Up : Vector2I.Zero;

        var target = current with
        {
            Parent = parent,
            Child = child,
            ParentPos = parentPos,
            ChildPos = childPos,
            Phase = OperationPhase.Operating,
            IsBetweenCells = false,
        };

        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.WaitingSpawn, current))
            {
                return;
            }
            context.IsLocked = true;

            (var parentNode, var parentAnim) = context.Board.SpawnBlock(parent, parentPos);
            parentNode.Outlined = true;
            (var childNode, var childAnim) = child is not null
                ? context.Board.SpawnBlock(child, childPos)
                : default;
            var anim = child is not null ? Task.WhenAll(parentAnim, childAnim) : parentAnim;

            context.Parent = parentNode;
            context.Child = childNode;

            context.OperationState = target;

            await anim;

            context.IsLocked = false;
        }

        return OperationResult.Succeeded(target, context.TrackAnim(PlayAnim));
    }
}

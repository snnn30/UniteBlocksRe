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
        if (!context.CanOperate(OperationPhase.WaitingSpawn))
        {
            return OperationResult.Failed();
        }

        var task = ApplyAndAnim(context, parent, child);
        return OperationResult.Succeeded(task);
    }

    private static async Task ApplyAndAnim(
        OperationContext context,
        BlockEntity parent,
        BlockEntity child
    )
    {
        context.IsLocked = true;

        var parentPos = BoardEntity.SpawnPosition;
        var childPos = child != null ? parentPos + Vector2I.Up : Vector2I.Zero;

        (var parentNode, var parentAnim) = context.Board.SpawnBlock(parent, parentPos);
        parentNode.Outlined = true;
        (var childNode, var childAnim) =
            child != null ? context.Board.SpawnBlock(child, childPos) : (null, Task.CompletedTask);

        context.Board.BringToFront(parentNode);

        context.Parent = parentNode;
        context.ParentPos = parentPos;
        context.Child = childNode;
        if (childNode is not null)
        {
            context.ChildPos = childPos;
            context.BasePoasitions.Child = childNode.Position;
        }
        context.Phase = OperationPhase.Operating;
        context.IsBetweenCells = false;
        context.Offsets.Clear();
        context.BasePoasitions.Parent = parentNode.Position;

        var anim = child is not null ? Task.WhenAll(parentAnim, childAnim) : parentAnim;
        await context.TrackAnim(anim);

        context.IsLocked = false;
    }
}

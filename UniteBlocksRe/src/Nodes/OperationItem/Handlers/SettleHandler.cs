using System;
using System.Threading.Tasks;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class SettleHandler
{
    public static OperationResult Settle(OperationContext context)
    {
        var current = context.OperationState;

        if (!context.CanOperate(OperationPhase.Operating, current))
        {
            return OperationResult.Failed(current);
        }
        if (current.IsBetweenCells)
        {
            Log.Warn("マスの半分の高さにある");
            return OperationResult.Failed(current);
        }
        if (current.Parent is null)
        {
            Log.Warn("セット対象が存在しない");
            return OperationResult.Failed(current);
        }

        var target = current with
        {
            Parent = null,
            Child = null,
            Phase = OperationPhase.WaitingSpawn,
        };

        return OperationResult.Succeeded(target, CreateSettleAnimation(context, current, target));
    }

    private static Func<Task> CreateSettleAnimation(
        OperationContext context,
        OperationState current,
        OperationState target
    )
    {
        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.Operating, current))
            {
                return;
            }
            context.IsLocked = true;
            context.OperationState = target;

            await context.WaitForAnimations();
            await SettleAnimation(context, current);

            context.Parent = null;
            context.Child = null;

            foreach (NBlock block in context.OperationItem.GetChildren())
            {
                block.QueueFree();
            }

            context.IsLocked = false;
        }

        return context.TrackAnim(PlayAnim);
    }

    private static Task SettleAnimation(OperationContext context, OperationState current)
    {
        var parentAnim = context.Board.SetOnBoard(context.Parent, current.ParentPos);
        var childAnim =
            current.Child != null
                ? context.Board.SetOnBoard(context.Child, current.ChildPos)
                : Task.CompletedTask;

        return Task.WhenAll(parentAnim, childAnim);
    }
}

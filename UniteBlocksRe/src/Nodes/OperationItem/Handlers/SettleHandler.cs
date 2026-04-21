using System.Threading.Tasks;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class SettleHandler
{
    public static OperationResult Settle(OperationContext context)
    {
        if (!context.CanOperate(OperationPhase.Operating))
        {
            return OperationResult.Failed();
        }
        if (context.IsBetweenCells)
        {
            Log.Warn("マスの半分の高さにある");
            return OperationResult.Failed();
        }
        if (context.Parent is null)
        {
            Log.Warn("セット対象が存在しない");
            return OperationResult.Failed();
        }

        var task = ApplyAndAnim(context);
        return OperationResult.Succeeded(task);
    }

    private static async Task ApplyAndAnim(OperationContext context)
    {
        context.IsLocked = true;

        var snapshot = context.CreateSnapshot();

        var task = context.WaitForAnimations();
        var task2 = SettleAnimation(snapshot);

        context.Parent = null;
        context.Child = null;
        context.Phase = OperationPhase.WaitingSpawn;

        await task;
        snapshot.Parent.Outlined = false;
        await task2;

        context.IsLocked = false;
    }

    private static Task SettleAnimation(OperationContext snapshot)
    {
        var parentAnim = snapshot.Board.SetOnBoardAsync(snapshot.Parent, snapshot.ParentPos);
        var childAnim =
            snapshot.Child != null
                ? snapshot.Board.SetOnBoardAsync(snapshot.Child, snapshot.ChildPos)
                : Task.CompletedTask;

        return Task.WhenAll(parentAnim, childAnim);
    }
}

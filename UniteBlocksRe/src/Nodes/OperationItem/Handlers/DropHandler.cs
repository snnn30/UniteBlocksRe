using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class DropHandler
{
    public static OperationResult Drop(OperationContext context, bool isSingle)
    {
        return isSingle switch
        {
            true => Drop(context, 0.1f, Tween.TransitionType.Quart, Tween.EaseType.Out),
            false => Drop(context, 0.02f, Tween.TransitionType.Linear, Tween.EaseType.In),
        };
    }

    private static OperationResult Drop(
        OperationContext context,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        if (!context.CanOperate(OperationPhase.Operating))
        {
            return OperationResult.Failed();
        }
        if (!CanDrop(context))
        {
            return OperationResult.Failed();
        }

        var task = ApplyAndAnim(context, duration, trans, ease);
        return OperationResult.Succeeded(task);
    }

    private static bool CanDrop(OperationContext context)
    {
        if (context.IsBetweenCells)
        {
            return true;
        }

        var targetParentPos = context.ParentPos + Vector2I.Down;
        var canPlace = context.Board.Model.CanPlace(targetParentPos, context.Parent.Model);
        if (context.Child != null)
        {
            var targetChildPos = context.ChildPos + Vector2I.Down;
            canPlace &= context.Board.Model.CanPlace(targetChildPos, context.Child.Model);
        }

        return canPlace;
    }

    private static async Task ApplyAndAnim(
        OperationContext context,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        var snapshot = context.CreateSnapshot();

        if (!context.IsBetweenCells)
        {
            context.ParentPos += Vector2I.Down;
            context.ChildPos += Vector2I.Down;
        }

        context.IsBetweenCells = !context.IsBetweenCells;

        await context.TrackAnim(DropAnimation(snapshot, duration, trans, ease));
    }

    private static Task DropAnimation(
        OperationContext snapshot,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        var tween = snapshot.CreateTween().SetTrans(trans).SetEase(ease);
        var sum = Vector2.Zero;
        tween.TweenMethod(
            Callable.From<Vector2>(val =>
            {
                var diff = val - sum;
                snapshot.Parent.Position += diff;
                if (snapshot.Child != null)
                {
                    snapshot.Child.Position += diff;
                }
                sum = val;
            }),
            Vector2.Zero,
            Vector2.Down * NBlock.BaseSize * 0.5f,
            duration
        );

        return tween.WaitForFinished();
    }
}

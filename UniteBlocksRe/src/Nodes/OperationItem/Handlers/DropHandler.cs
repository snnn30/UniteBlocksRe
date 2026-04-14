using System;
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
        var current = context.OperationState;

        if (!context.CanOperate(OperationPhase.Operating, current))
        {
            return OperationResult.Failed(current);
        }

        if (!CanDrop(current, context))
        {
            return OperationResult.Failed(current);
        }

        var target = current.IsBetweenCells
            ? current with
            {
                IsBetweenCells = false,
            }
            : current with
            {
                ParentPos = current.ParentPos + Vector2I.Down,
                ChildPos = current.ChildPos + Vector2I.Down,
                IsBetweenCells = true,
            };

        var dropAnimation = CreateDropAnimation(context, current, target, duration, trans, ease);

        return OperationResult.Succeeded(target, dropAnimation);
    }

    private static bool CanDrop(OperationState current, OperationContext context)
    {
        if (current.IsBetweenCells)
        {
            return true;
        }

        var targetParentPos = current.ParentPos + Vector2I.Down;
        var canPlace = context.Board.Model.CanPlace(targetParentPos, context.Parent.Model);
        if (current.Child != null)
        {
            var targetChildPos = current.ChildPos + Vector2I.Down;
            canPlace &= context.Board.Model.CanPlace(targetChildPos, context.Child.Model);
        }

        return canPlace;
    }

    private static Func<Task> CreateDropAnimation(
        OperationContext context,
        OperationState current,
        OperationState target,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.Operating, current))
            {
                return;
            }
            context.OperationState = target;
            await DropAnimation(context, duration, trans, ease);
        }

        return context.TrackAnim(PlayAnim);
    }

    private static Task DropAnimation(
        OperationContext context,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        var tween = context.OperationItem.CreateTween().SetTrans(trans).SetEase(ease);
        var sum = Vector2.Zero;
        tween.TweenMethod(
            Callable.From<Vector2>(val =>
            {
                var diff = val - sum;
                context.Parent.Position += diff;
                if (context.Child != null)
                {
                    context.Child.Position += diff;
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

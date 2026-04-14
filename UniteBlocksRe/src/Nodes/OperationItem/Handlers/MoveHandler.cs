using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class MoveHandler
{
    public static OperationResult Move(OperationContext context, bool isRight)
    {
        var current = context.OperationState;

        if (!context.CanOperate(OperationPhase.Operating, current))
        {
            return OperationResult.Failed(current);
        }

        var direction = isRight ? Vector2I.Right : Vector2I.Left;

        if (!CanMove(current, context, direction))
        {
            return OperationResult.Failed(current);
        }

        var target = current with
        {
            ParentPos = current.ParentPos + direction,
            ChildPos = current.ChildPos + direction,
        };

        var playAnimation = CreateMoveAnimation(context, current, target, direction);

        return OperationResult.Succeeded(target, playAnimation);
    }

    private static bool CanMove(
        OperationState current,
        OperationContext context,
        Vector2I direction
    )
    {
        var targetParentPos = current.ParentPos + direction;
        var canPlace = context.Board.Model.CanPlace(targetParentPos, context.Parent.Model);
        canPlace &=
            context.Board.Model.CanPlace(targetParentPos + Vector2I.Up, context.Parent.Model)
            || !current.IsBetweenCells;

        if (context.Child is not null)
        {
            var targetChildPos = current.ChildPos + direction;
            canPlace &= context.Board.Model.CanPlace(targetChildPos, context.Child.Model);
            canPlace &=
                context.Board.Model.CanPlace(targetChildPos + Vector2I.Up, context.Child.Model)
                || !current.IsBetweenCells;
        }

        return canPlace;
    }

    private static Func<Task> CreateMoveAnimation(
        OperationContext context,
        OperationState current,
        OperationState target,
        Vector2I direction
    )
    {
        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.Operating, current))
            {
                return;
            }

            context.OperationState = target;
            await MoveAnimation(context, direction);
        }

        return context.TrackAnim(PlayAnim);
    }

    private static Task MoveAnimation(OperationContext context, Vector2I direction)
    {
        var tween = context
            .OperationItem.CreateTween()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
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
            (Vector2)direction * NBlock.BaseSize,
            0.03f
        );

        return tween.WaitForFinished();
    }
}

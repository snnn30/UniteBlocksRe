using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class RotateHandler
{
    public static OperationResult Rotate(OperationContext context, bool isCW)
    {
        const float Duration = 0.2f;

        var current = context.OperationState;

        if (!context.CanOperate(OperationPhase.Operating, current))
        {
            return OperationResult.Failed(current);
        }

        if (current.Child == null)
        {
            return OperationResult.Succeeded(current, () => Task.CompletedTask);
        }

        // trueの所は何でもいい ダミーの値を入れる
        return true switch
        {
            _ when CanNormalRotate(current, context, isCW, false) is (true, var target) => new(
                true,
                target,
                CreateNormalRotateAnimation(context, current, target, isCW, Duration, false)
            ),
            _ when CanShiftRotate(current, context, isCW, false) is (true, var target) => new(
                true,
                target,
                CreateShiftRotateAnimation(context, current, target, isCW, Duration, false)
            ),
            _ when CanNormalRotate(current, context, isCW, true) is (true, var target) => new(
                true,
                target,
                CreateNormalRotateAnimation(context, current, target, isCW, Duration, true)
            ),
            _ when CanShiftRotate(current, context, isCW, true) is (true, var target) => new(
                true,
                target,
                CreateShiftRotateAnimation(context, current, target, isCW, Duration, true)
            ),
            _ => OperationResult.Failed(current),
        };
    }

    private static (bool CanRotate, OperationState target) CanNormalRotate(
        OperationState current,
        OperationContext context,
        bool isCW,
        bool pivotIsChild
    )
    {
        var pivotPos = pivotIsChild ? current.ChildPos : current.ParentPos;
        var moverPos = pivotIsChild ? current.ParentPos : current.ChildPos;
        var moverModel = pivotIsChild ? context.Parent.Model : context.Child.Model;

        var relativePos = moverPos - pivotPos;
        var rotatedRelative = isCW
            ? new Vector2I(-relativePos.Y, relativePos.X)
            : new Vector2I(relativePos.Y, -relativePos.X);

        var targetMoverPos = pivotPos + rotatedRelative;

        var canPlace = context.Board.Model.CanPlace(targetMoverPos, moverModel);
        canPlace &=
            context.Board.Model.CanPlace(targetMoverPos + Vector2I.Up, moverModel)
            || !current.IsBetweenCells;
        if (!canPlace)
        {
            return (false, default);
        }

        var target = pivotIsChild
            ? current with
            {
                ParentPos = targetMoverPos,
            }
            : current with
            {
                ChildPos = targetMoverPos,
            };
        return (true, target);
    }

    private static (bool CanRotate, OperationState target) CanShiftRotate(
        OperationState current,
        OperationContext context,
        bool isCW,
        bool pivotIsChild
    )
    {
        var pivotPos = pivotIsChild ? current.ChildPos : current.ParentPos;
        var moverPos = pivotIsChild ? current.ParentPos : current.ChildPos;
        var moverModel = pivotIsChild ? context.Parent.Model : context.Child.Model;

        var relativePos = moverPos - pivotPos;
        var relativeWallPos = isCW
            ? new Vector2I(-relativePos.Y, relativePos.X)
            : new Vector2I(relativePos.Y, -relativePos.X);
        var targetMoverPos = pivotPos + relativePos + relativeWallPos;

        var canPlace = context.Board.Model.CanPlace(targetMoverPos, moverModel);
        canPlace &= !context.Board.Model.CanPlace(pivotPos + relativeWallPos, moverModel);
        canPlace &=
            context.Board.Model.CanPlace(targetMoverPos + Vector2I.Up, moverModel)
            || !current.IsBetweenCells;
        if (canPlace is false)
        {
            return (false, default);
        }
        var target = pivotIsChild
            ? current with
            {
                ChildPos = current.ParentPos,
                ParentPos = targetMoverPos,
            }
            : current with
            {
                ParentPos = current.ChildPos,
                ChildPos = targetMoverPos,
            };
        return (true, target);
    }

    private static Func<Task> CreateShiftRotateAnimation(
        OperationContext context,
        OperationState current,
        OperationState target,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.Operating, current))
            {
                return;
            }

            context.OperationState = target;
            await ShiftRotateAnimation(context, current, isCW, duration, pivotIsChild);
        }

        return context.TrackAnim(PlayAnim);
    }

    private static Func<Task> CreateNormalRotateAnimation(
        OperationContext context,
        OperationState current,
        OperationState target,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        async Task PlayAnim()
        {
            if (!context.CanOperate(OperationPhase.Operating, current))
            {
                return;
            }

            context.OperationState = target;
            await NormalRotateAnimation(context, isCW, duration, pivotIsChild);
        }

        return context.TrackAnim(PlayAnim);
    }

    private static Task ShiftRotateAnimation(
        OperationContext context,
        OperationState current,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var task = NormalRotateAnimation(context, isCW, duration, pivotIsChild);
        var tween = context
            .OperationItem.CreateTween()
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);

        var sum = Vector2.Zero;
        var relativePos = pivotIsChild
            ? current.ParentPos - current.ChildPos
            : current.ChildPos - current.ParentPos;
        tween.TweenMethod(
            Callable.From<Vector2>(val =>
            {
                var diff = val - sum;
                context.Parent.Position += diff;
                context.Child.Position += diff;
                sum = val;
            }),
            Vector2.Zero,
            (Vector2)relativePos * NBlock.BaseSize,
            duration
        );

        var task2 = tween.WaitForFinished();

        return Task.WhenAll(task, task2);
    }

    private static Task NormalRotateAnimation(
        OperationContext context,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var tween = context
            .OperationItem.CreateTween()
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);
        var sum = 0f;
        tween.TweenMethod(
            Callable.From<float>(deg =>
            {
                var diff = deg - sum;
                var radDiff = Mathf.DegToRad(diff);
                if (pivotIsChild)
                {
                    var relativePos = context.Parent.Position - context.Child.Position;
                    context.Parent.Position = context.Child.Position + relativePos.Rotated(radDiff);
                }
                else
                {
                    var relativePos = context.Child.Position - context.Parent.Position;
                    context.Child.Position = context.Parent.Position + relativePos.Rotated(radDiff);
                }

                sum = deg;
            }),
            0f,
            isCW ? 90f : -90f,
            duration
        );

        return tween.WaitForFinished();
    }
}

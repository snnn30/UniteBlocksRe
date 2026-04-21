using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class RotateHandler
{
    public static OperationResult Rotate(OperationContext context, bool isCW, float duration)
    {
        if (!context.CanOperate(OperationPhase.Operating))
        {
            return OperationResult.Failed();
        }
        if (context.Child == null)
        {
            return OperationResult.Succeeded(Task.CompletedTask);
        }

        // trueの所は何でもいい ダミーの値を入れる
        // 各caseごとに独立したスコープが作成される
        return true switch
        {
            _ when CanNormalRotate(context, isCW, false) is (true, var parentPos, var childPos) =>
                OperationResult.Succeeded(
                    ApplyAndAnimNormal(context, parentPos, childPos, isCW, duration, false)
                ),
            _ when CanShiftRotate(context, isCW, false) is (true, var parentPos, var childPos) =>
                OperationResult.Succeeded(
                    ApplyAndAnimShift(context, parentPos, childPos, isCW, duration, false)
                ),
            _ when CanNormalRotate(context, isCW, true) is (true, var parentPos, var childPos) =>
                OperationResult.Succeeded(
                    ApplyAndAnimNormal(context, parentPos, childPos, isCW, duration, true)
                ),
            _ when CanShiftRotate(context, isCW, true) is (true, var parentPos, var childPos) =>
                OperationResult.Succeeded(
                    ApplyAndAnimShift(context, parentPos, childPos, isCW, duration, true)
                ),
            _ => OperationResult.Failed(),
        };
    }

    private static (
        bool CanRotate,
        Vector2I TargetParentPos,
        Vector2I TargetChildPos
    ) CanNormalRotate(OperationContext context, bool isCW, bool pivotIsChild)
    {
        var pivotPos = pivotIsChild ? context.ChildPos : context.ParentPos;
        var moverPos = pivotIsChild ? context.ParentPos : context.ChildPos;
        var moverModel = pivotIsChild ? context.Parent.Model : context.Child.Model;

        var relativePos = moverPos - pivotPos;
        var rotatedRelative = isCW
            ? new Vector2I(-relativePos.Y, relativePos.X)
            : new Vector2I(relativePos.Y, -relativePos.X);

        var targetMoverPos = pivotPos + rotatedRelative;

        var canPlace = context.Board.Model.CanPlace(targetMoverPos, moverModel);
        canPlace &=
            context.Board.Model.CanPlace(targetMoverPos + Vector2I.Up, moverModel)
            || !context.IsBetweenCells;
        if (!canPlace)
        {
            return (false, context.ParentPos, context.ChildPos);
        }

        var targetParentPos = pivotIsChild ? targetMoverPos : context.ParentPos;
        var targetChildPos = pivotIsChild ? context.ChildPos : targetMoverPos;

        return (true, targetParentPos, targetChildPos);
    }

    private static (
        bool CanRotate,
        Vector2I TargetParentPos,
        Vector2I TargetChildPos
    ) CanShiftRotate(OperationContext context, bool isCW, bool pivotIsChild)
    {
        var pivotPos = pivotIsChild ? context.ChildPos : context.ParentPos;
        var moverPos = pivotIsChild ? context.ParentPos : context.ChildPos;
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
            || !context.IsBetweenCells;
        if (canPlace is false)
        {
            return (false, context.ParentPos, context.ChildPos);
        }

        var targetParentPos = pivotIsChild ? targetMoverPos : context.ChildPos;
        var targetChildPos = pivotIsChild ? context.ParentPos : targetMoverPos;

        return (true, targetParentPos, targetChildPos);
    }

    private static async Task ApplyAndAnimShift(
        OperationContext context,
        Vector2I targetParentPos,
        Vector2I targetChildPos,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var snapshot = context.CreateSnapshot();

        context.ParentPos = targetParentPos;
        context.ChildPos = targetChildPos;

        await context.TrackAnim(ShiftRotateAnimation(snapshot, isCW, duration, pivotIsChild));
    }

    private static async Task ApplyAndAnimNormal(
        OperationContext context,
        Vector2I targetParentPos,
        Vector2I targetChildPos,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var snapshot = context.CreateSnapshot();

        context.ParentPos = targetParentPos;
        context.ChildPos = targetChildPos;

        await context.TrackAnim(NormalRotateAnimation(snapshot, isCW, duration, pivotIsChild));
    }

    private static async Task ShiftRotateAnimation(
        OperationContext snapshot,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var task = NormalRotateAnimation(snapshot, isCW, duration, pivotIsChild);
        var tween = snapshot
            .CreateTween()
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);

        var relativePos = pivotIsChild
            ? snapshot.ParentPos - snapshot.ChildPos
            : snapshot.ChildPos - snapshot.ParentPos;

        var parentHandler = snapshot.Parent.AddOffset();
        var childHandler = snapshot.Child.AddOffset();

        tween.TweenMethod(
            Callable.From<Vector2>(v =>
            {
                parentHandler.Val = v;
                childHandler.Val = v;
            }),
            Vector2.Zero,
            (Vector2)relativePos * NBlock.BaseSize,
            duration
        );

        var task2 = tween.WaitForFinished();
        await Task.WhenAll(task, task2);
        parentHandler.Apply();
        childHandler.Apply();
    }

    private static async Task NormalRotateAnimation(
        OperationContext snapshot,
        bool isCW,
        float duration,
        bool pivotIsChild
    )
    {
        var tween = snapshot
            .CreateTween()
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);

        Vector2 relativePos = pivotIsChild
            ? (snapshot.ParentPos - snapshot.ChildPos) * NBlock.BaseSize
            : (snapshot.ChildPos - snapshot.ParentPos) * NBlock.BaseSize;

        var parentHandler = snapshot.Parent.AddOffset();
        var childHandler = snapshot.Child.AddOffset();

        tween.TweenMethod(
            Callable.From<float>(deg =>
            {
                var rad = Mathf.DegToRad(deg);
                var rotatedOffset = relativePos.Rotated(rad);
                var targetOffset = rotatedOffset - relativePos;

                if (pivotIsChild)
                {
                    parentHandler.Val = targetOffset;
                }
                else
                {
                    childHandler.Val = targetOffset;
                }
            }),
            0f,
            isCW ? 90f : -90f,
            duration
        );

        await tween.WaitForFinished();
        parentHandler.Apply();
        childHandler.Apply();
    }
}

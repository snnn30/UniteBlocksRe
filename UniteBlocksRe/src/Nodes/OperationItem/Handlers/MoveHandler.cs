using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Extensions;

namespace UniteBlocksRe.src.Nodes.OperationItem.Handlers;

public static class MoveHandler
{
    public static OperationResult Move(OperationContext context, bool isRight, float duration)
    {
        if (!context.CanOperate(OperationPhase.Operating))
        {
            return OperationResult.Failed();
        }
        var direction = isRight ? Vector2I.Right : Vector2I.Left;
        if (!CanMove(context, direction))
        {
            return OperationResult.Failed();
        }

        var snapshot = context.CreateSnapshot();
        Apply(context, direction);
        var task = context.TrackAnim(MoveAnimation(snapshot, direction, duration));
        return OperationResult.Succeeded(task);
    }

    private static bool CanMove(OperationContext context, Vector2I direction)
    {
        var targetParentPos = context.ParentPos + direction;
        var canPlace = context.Board.Model.CanPlace(targetParentPos, context.Parent.Model);
        canPlace &=
            context.Board.Model.CanPlace(targetParentPos + Vector2I.Up, context.Parent.Model)
            || !context.IsBetweenCells;

        if (context.Child is not null)
        {
            var targetChildPos = context.ChildPos + direction;
            canPlace &= context.Board.Model.CanPlace(targetChildPos, context.Child.Model);
            canPlace &=
                context.Board.Model.CanPlace(targetChildPos + Vector2I.Up, context.Child.Model)
                || !context.IsBetweenCells;
        }

        return canPlace;
    }

    private static void Apply(OperationContext context, Vector2I direction)
    {
        context.ParentPos += direction;
        context.ChildPos += direction;
    }

    private static async Task MoveAnimation(
        OperationContext snapshot,
        Vector2I direction,
        float duration
    )
    {
        var tween = snapshot
            .CreateTween()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        var parentHandler = snapshot.Parent.AddOffset();
        var childHandler = snapshot.Child?.AddOffset();

        tween.TweenMethod(
            Callable.From<Vector2>(v =>
            {
                parentHandler.Val = v;
                if (snapshot.HasChild)
                {
                    childHandler.Val = v;
                }
            }),
            Vector2.Zero,
            (Vector2)direction * NBlock.BaseSize,
            duration
        );

        await tween.WaitForFinished();
        parentHandler.Apply();
        childHandler?.Apply();
    }
}

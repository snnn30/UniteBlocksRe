using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class MoveHandler
{
    public static OperationResult Move(OperationContext context, bool isRight)
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

        var task = ApplyAndAnim(context, direction);
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

    private static async Task ApplyAndAnim(OperationContext context, Vector2I direction)
    {
        var snapshot = context.CreateSnapshot();

        context.ParentPos += direction;
        context.ChildPos += direction;

        await context.TrackAnim(MoveAnimation(snapshot, direction));
    }

    private static async Task MoveAnimation(OperationContext snapshot, Vector2I direction)
    {
        var tween = snapshot
            .CreateTween()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        var offset = new RealPositions();
        snapshot.Offsets.Add(offset);
        tween.TweenMethod(
            Callable.From<Vector2>(val =>
            {
                offset.Parent = val;
                if (snapshot.Child != null)
                {
                    offset.Child = val;
                }
            }),
            Vector2.Zero,
            (Vector2)direction * NBlock.BaseSize,
            0.06f
        );

        await tween.WaitForFinished();
        snapshot.Offsets.Remove(offset);
        snapshot.BasePoasitions.Add(offset);
    }
}

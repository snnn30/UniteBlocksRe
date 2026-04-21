using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;

namespace UniteBlocksRe.Nodes.OperationItem.Handlers;

public static class DropHandler
{
    public static OperationResult Drop(OperationContext context, float duration)
    {
        if (!context.CanOperate(OperationPhase.Operating))
        {
            return OperationResult.Failed();
        }
        if (!CanDrop(context))
        {
            return OperationResult.Failed();
        }

        var snapshot = context.CreateSnapshot();
        Apply(context);
        var task = context.TrackAnim(PlayAnim(snapshot, duration));
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

    private static void Apply(OperationContext context)
    {
        if (!context.IsBetweenCells)
        {
            context.ParentPos += Vector2I.Down;
            context.ChildPos += Vector2I.Down;
        }
        context.IsBetweenCells = !context.IsBetweenCells;
    }

    private static async Task PlayAnim(OperationContext snapshot, float duration)
    {
        var tween = snapshot
            .CreateTween()
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        var parentHandler = snapshot.Parent.AddOffset();
        var childHandler = snapshot.Child?.AddOffset();

        tween.TweenMethod(
            Callable.From<Vector2>(v =>
            {
                parentHandler.Val = v;
                if (childHandler != null)
                {
                    childHandler.Val = v;
                }
            }),
            Vector2.Zero,
            Vector2.Down * NBlock.BaseSize * 0.5f,
            duration
        );

        await tween.WaitForFinished();
        parentHandler.Apply();
        childHandler?.Apply();
    }
}

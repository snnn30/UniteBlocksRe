using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Nodes.OperationItem;

namespace UniteBlocksRe.src.Nodes.OperationItem.Handlers;

public static class SpawnHandler
{
    public static OperationResult Spawn(
        OperationContext context,
        BlockEntity parent,
        BlockEntity child = null
    )
    {
        if (!context.CanOperate(OperationPhase.WaitingSpawn))
        {
            Log.Warn("フェーズが不正");
            return OperationResult.Failed();
        }
        if (!context.Board.Model.CanPlace(BoardEntity.SpawnPosition, Vector2I.One))
        {
            Log.Info("スポーン位置が埋まっている");
            return OperationResult.Failed();
        }

        var task = ApplyAndAnim(context, parent, child);
        return OperationResult.Succeeded(task);
    }

    private static async Task ApplyAndAnim(
        OperationContext context,
        BlockEntity parent,
        BlockEntity child
    )
    {
        context.IsLocked = true;

        var parentPos = BoardEntity.SpawnPosition;
        var childPos = child != null ? parentPos + Vector2I.Up : Vector2I.Zero;

        var parentNode = NBlock.Create(parent);
        parentNode.Outlined = true;
        context.Board.AddAsBoardElement(parentNode);
        parentNode.Position = NBoard.GetRealPosition(parentPos);

        NBlock childNode = null;
        if (child != null)
        {
            childNode = NBlock.Create(child);
            context.Board.AddAsBoardElement(childNode);
            childNode.Position = NBoard.GetRealPosition(childPos);
        }
        context.Board.BringToFront(parentNode);

        context.Parent = parentNode;
        context.ParentPos = parentPos;
        context.Child = childNode;
        context.ChildPos = childPos;
        context.Phase = OperationPhase.Operating;
        context.IsBetweenCells = false;

        var parentAnim = parentNode.PlaySpawnAnimeAsync();
        var childAnim = child == null ? Task.CompletedTask : childNode.PlaySpawnAnimeAsync();
        var anim = Task.WhenAll(parentAnim, childAnim);
        await context.TrackAnim(anim);

        context.IsLocked = false;
    }
}

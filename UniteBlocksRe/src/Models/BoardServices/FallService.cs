using System.Collections.Generic;
using System.Linq;
using Godot;

namespace UniteBlocksRe.src.Models.BoardServices;

public static class FallService
{
    public static FallResult Execute(BoardEntity board)
    {
        var movements = new List<Movement>();

        // 下にあるブロックから順に確定させていく（Yの降順）
        var sortedBlocks = board.OrderByDescending(bp => bp.Pos.Y).Select(b => b.Block);

        foreach (var block in sortedBlocks)
        {
            ProcessSingleBlockFall(board, block, movements);
        }

        return new FallResult(movements);
    }

    /// <summary>
    /// 指定されたブロックを限界まで落下させる
    /// </summary>
    private static void ProcessSingleBlockFall(
        BoardEntity board,
        BlockEntity block,
        List<Movement> movements
    )
    {
        var startPos = board.GetPositionOf(block);
        board.Remove(block);
        var fallCount = 0;
        while (true)
        {
            if (board.CanPlace(startPos + Vector2I.Down * (fallCount + 1), block))
            {
                fallCount++;
            }
            else
            {
                break;
            }
        }

        var targetPos = startPos + Vector2I.Down * fallCount;
        board.Place(targetPos, block);

        if (fallCount > 0)
        {
            movements.Add(new Movement(block, startPos, targetPos));
        }
    }
}

public sealed record FallResult(IReadOnlyList<Movement> Movements) : IProcessStep
{
    public bool HasFalled => Movements.Count > 0;
}

public readonly record struct Movement(BlockEntity Block, Vector2I From, Vector2I To);

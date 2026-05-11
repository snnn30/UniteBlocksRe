using System.Collections.Generic;
using System.Linq;
using Godot;

namespace UniteBlocksRe.Models.BoardServices;

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

    // 指定されたブロックを限界まで落下させる
    private static void ProcessSingleBlockFall(
        BoardEntity board,
        BlockEntity block,
        List<Movement> movements
    )
    {
        var startPos = board.GetPositionOf(block);
        var currentPos = startPos;
        board.Remove(block);

        while (board.CanPlace(currentPos + Vector2I.Down, block))
        {
            currentPos += Vector2I.Down;
        }
        board.Place(currentPos, block);

        if (startPos != currentPos)
        {
            movements.Add(new Movement(block, startPos, currentPos));
        }
    }
}

public sealed record FallResult(IReadOnlyList<Movement> Movements) : IProcessStep
{
    public bool HasFalled => Movements.Count > 0;
}

public readonly record struct Movement(BlockEntity Block, Vector2I From, Vector2I To);

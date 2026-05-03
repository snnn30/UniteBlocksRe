using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.Boards.Operations;

public static class FallService
{
    public static FallResult Execute(Board initialState)
    {
        var movements = new List<Movement>();
        var currentState = initialState;

        // 下にあるブロックから順に確定させていく（Yの降順）
        var blocks = currentState.Grid.Values;
        var sortedBlocks = blocks.OrderByDescending(b => initialState.GetPositionOf(b).Y);

        foreach (var block in sortedBlocks)
        {
            currentState = ProcessSingleBlockFall(currentState, block, movements);
        }

        return new FallResult(initialState, currentState, movements);
    }

    /// <summary>
    /// 指定されたブロックを限界まで落下させ、更新後の盤面を返す
    /// </summary>
    private static Board ProcessSingleBlockFall(Board board, Block block, List<Movement> movements)
    {
        var startPos = board.GetPositionOf(block);
        var currentBoard = board.Remove(block);
        var fallCount = 0;
        while (true)
        {
            if (currentBoard.CanPlace(startPos + Vector2I.Down * (fallCount + 1), block))
            {
                fallCount++;
            }
            else
            {
                break;
            }
        }

        var targetPos = startPos + Vector2I.Down * fallCount;
        currentBoard = currentBoard.Place(targetPos, block);

        if (fallCount > 0)
        {
            movements.Add(new Movement(block, startPos, targetPos));
        }

        return currentBoard;
    }
}

public sealed record FallResult(Board Before, Board After, IReadOnlyList<Movement> Movements)
    : BoardOperationStep(Before, After)
{
    public bool HasFalled => Movements.Count > 0;
}

public readonly record struct Movement(Block Block, Vector2I From, Vector2I To);

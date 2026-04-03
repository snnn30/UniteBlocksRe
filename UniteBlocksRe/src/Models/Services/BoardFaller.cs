using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Services;

public static class BoardFaller
{
    public static FallResult Fall(BoardEntity board)
    {
        var list = new List<BlockEntity>();
        for (var y = BoardEntity.Size.Y - 1; y >= 0; y--)
        {
            for (var x = BoardEntity.Size.X - 1; x >= 0; x--)
            {
                (var sucess, var block) = board.TryGetBlock(new Vector2I(x, y));
                if (sucess && !list.Contains(block))
                {
                    list.Add(block);
                }
            }
        }

        var movements = new List<FallStep>();
        foreach (var block in list)
        {
            if (TryFallSingleBlock(board, block) is (true, var step))
            {
                movements.Add(step);
            }
        }

        return new FallResult(movements);
    }

    private static (bool Sucess, FallStep step) TryFallSingleBlock(
        BoardEntity board,
        BlockEntity block
    )
    {
        var startPos = board.TryGetOrigin(block).Position;
        var targetPos = startPos;

        board.TryRemoveBlock(block);

        for (var y = startPos.Y + 1; y < BoardEntity.Size.Y; y++)
        {
            Vector2I newPos = new(startPos.X, y);
            if (board.CanPlace(newPos, block))
            {
                targetPos = newPos;
            }
            else
            {
                break;
            }
        }

        board.TrySetBlock(targetPos, block);
        if (targetPos != startPos)
        {
            return (true, new FallStep(block, startPos, targetPos));
        }
        else
        {
            return (false, null);
        }
    }
}

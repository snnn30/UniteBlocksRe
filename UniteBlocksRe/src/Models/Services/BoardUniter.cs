using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Services;

public static class BoardUniter
{
    public static UniteResult Unite(BoardEntity board)
    {
        var result = new List<UniteStep>();

        for (var y = 0; y < BoardEntity.Size.Y; y++)
        {
            for (var x = 0; x < BoardEntity.Size.X; x++)
            {
                var pos = new Vector2I(x, y);
                var (exists, block) = board.TryGetBlock(pos);
                if (!exists)
                {
                    continue;
                }

                var origin = board.TryGetOrigin(block).Position;
                if (origin != pos)
                {
                    continue;
                }

                var targetSize = CalculateLargestRectangle(board, block);
                if (targetSize == block.Size || targetSize.X < 2 || targetSize.Y < 2)
                {
                    continue;
                }

                var step = PerformUnite(board, origin, targetSize, block.Color);
                result.Add(step);
            }
        }

        return new UniteResult(result);
    }

    private static UniteStep PerformUnite(
        BoardEntity board,
        Vector2I origin,
        Vector2I shape,
        BlockColor color
    )
    {
        var targets = new HashSet<BlockEntity>();

        for (var y = origin.Y; y < origin.Y + shape.Y; y++)
        {
            for (var x = origin.X; x < origin.X + shape.X; x++)
            {
                if (board.TryGetBlock(new(x, y)) is (true, var block))
                {
                    targets.Add(block);
                }
            }
        }

        foreach (var target in targets)
        {
            board.TryRemoveBlock(target);
        }

        var createdBlock = new BlockEntity(color, shape);
        board.TrySetBlock(origin, createdBlock);

        return new UniteStep(targets, createdBlock, origin);
    }

    private static Vector2I CalculateLargestRectangle(BoardEntity board, BlockEntity startBlock)
    {
        var largestSize = startBlock.Size;

        var color = startBlock.Color;
        var origin = board.TryGetOrigin(startBlock).Position;

        for (var y = startBlock.Size.Y; origin.Y + y <= BoardEntity.Size.Y; y++)
        {
            for (var x = startBlock.Size.X; origin.X + x <= BoardEntity.Size.X; x++)
            {
                if (x * y <= largestSize.X * largestSize.Y)
                {
                    continue;
                }

                if (CanRangeFill(board, origin, new(x, y), color))
                {
                    largestSize.X = x;
                    largestSize.Y = y;
                }
            }
        }

        return largestSize;
    }

    private static bool CanRangeFill(
        BoardEntity board,
        Vector2I origin,
        Vector2I shape,
        BlockColor color
    )
    {
        for (var y = origin.Y; y < origin.Y + shape.Y; y++)
        {
            for (var x = origin.X; x < origin.X + shape.X; x++)
            {
                if (
                    board.TryGetBlock(new Vector2I(x, y)) is not (true, var block)
                    || block.Color != color
                )
                {
                    return false;
                }

                var pos = board.TryGetOrigin(block).Position;

                if (
                    pos.X < origin.X
                    || pos.X + block.Size.X > origin.X + shape.X
                    || pos.Y < origin.Y
                    || pos.Y + block.Size.Y > origin.Y + shape.Y
                )
                {
                    return false;
                }
            }
        }

        return true;
    }
}

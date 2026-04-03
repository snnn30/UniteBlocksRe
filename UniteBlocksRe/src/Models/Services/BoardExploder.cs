using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Services;

public static class BoardExploder
{
    public static ExplodeResult Explode(BoardEntity board, BlockEntity block)
    {
        var steps = new List<ExplodeStep>();
        var currentTarget = new HashSet<BlockEntity> { block };

        while (currentTarget.Count > 0)
        {
            var explodedInThisStep = new HashSet<BlockEntity>(currentTarget);
            var nextNeighbors = FindNextNeighbors(board, explodedInThisStep);
            ApplyExplode(board, explodedInThisStep);
            steps.Add(new ExplodeStep(explodedInThisStep));
            currentTarget = nextNeighbors;
        }

        return new ExplodeResult(steps);
    }

    private static HashSet<BlockEntity> FindNextNeighbors(
        BoardEntity board,
        HashSet<BlockEntity> currentStep
    )
    {
        var neighbors = new HashSet<BlockEntity>();
        foreach (var block in currentStep)
        {
            neighbors.UnionWith(GetAdjacentSameTypeBlocks(board, block));
        }
        neighbors.ExceptWith(currentStep);
        return neighbors;
    }

    private static void ApplyExplode(BoardEntity board, IEnumerable<BlockEntity> blocks)
    {
        foreach (var b in blocks)
        {
            board.TryRemoveBlock(b);
        }
    }

    private static List<BlockEntity> GetAdjacentSameTypeBlocks(
        BoardEntity board,
        BlockEntity centerBlock
    )
    {
        var origin = board.TryGetOrigin(centerBlock).Position;

        var occupied = new List<Vector2I>();
        for (var dx = 0; dx < centerBlock.Size.X; dx++)
        {
            for (var dy = 0; dy < centerBlock.Size.Y; dy++)
            {
                occupied.Add(new Vector2I(origin.X + dx, origin.Y + dy));
            }
        }

        (int, int)[] directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
        var results = new List<BlockEntity>();

        foreach (var pos in occupied)
        {
            foreach ((var dx, var dy) in directions)
            {
                var checkPos = new Vector2I(pos.X + dx, pos.Y + dy);
                (var success, var foundBlock) = board.TryGetBlock(checkPos);

                if (success && foundBlock != centerBlock && foundBlock.Color == centerBlock.Color)
                {
                    results.Add(foundBlock);
                }
            }
        }
        return results;
    }
}

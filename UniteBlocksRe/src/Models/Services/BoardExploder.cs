using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Services;

public static class BoardExploder
{
    public static ExplodeResult Explode(BoardEntity board, BlockEntity bomb)
    {
        if (bomb.Type != BlockType.Bomb)
        {
            Log.Warn("爆発起点がBombじゃない");
            return new ExplodeResult([]);
        }

        if (board.TryGetOrigin(bomb) is not (true, var bombPos))
        {
            Log.Warn("Bombがボード上に存在しない");
            return new ExplodeResult([]);
        }

        var steps = new List<ExplodeStep> { new([bomb]) };
        board.TryRemoveBlock(bomb);

        if (board.TryGetBlock(bombPos + Vector2I.Down) is not (true, var firsetTarget))
        {
            return new ExplodeResult(steps);
        }

        var currentChainOrigin = new HashSet<BlockEntity>();
        var pendingObstacles = new HashSet<BlockEntity>();

        if (firsetTarget.Type == BlockType.Obstacle)
        {
            pendingObstacles.Add(firsetTarget);
        }
        else if (firsetTarget.Type == BlockType.Normal)
        {
            currentChainOrigin.Add(firsetTarget);
        }

        while (currentChainOrigin.Count > 0 || pendingObstacles.Count > 0)
        {
            var explodedInThisStep = new HashSet<BlockEntity>(currentChainOrigin);
            explodedInThisStep.UnionWith(pendingObstacles);

            var (nextNeighbors, nextObstacles) = FindNextNeighbors(
                board,
                currentChainOrigin,
                pendingObstacles
            );

            ApplyExplode(board, explodedInThisStep);
            steps.Add(new ExplodeStep(explodedInThisStep));

            currentChainOrigin = nextNeighbors;
            pendingObstacles = nextObstacles;
        }

        return new ExplodeResult(steps);
    }

    private static (
        HashSet<BlockEntity> nextOrigins,
        HashSet<BlockEntity> obstacles
    ) FindNextNeighbors(
        BoardEntity board,
        HashSet<BlockEntity> currentOrigins,
        HashSet<BlockEntity> currentObstacles
    )
    {
        var nextOrigins = new HashSet<BlockEntity>();
        var obstacles = new HashSet<BlockEntity>();

        foreach (var block in currentOrigins)
        {
            var adjacents = GetAdjacentSameTypeBlocks(board, block);
            nextOrigins.UnionWith(adjacents.sameTypes);
            obstacles.UnionWith(adjacents.obstacles);
        }
        nextOrigins.ExceptWith(currentOrigins);
        obstacles.ExceptWith(currentObstacles);
        return (nextOrigins, obstacles);
    }

    private static void ApplyExplode(BoardEntity board, IEnumerable<BlockEntity> blocks)
    {
        foreach (var b in blocks)
        {
            board.TryRemoveBlock(b);
        }
    }

    private static (
        HashSet<BlockEntity> sameTypes,
        HashSet<BlockEntity> obstacles
    ) GetAdjacentSameTypeBlocks(BoardEntity board, BlockEntity centerBlock)
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
        var sameTypes = new HashSet<BlockEntity>();
        var obstacles = new HashSet<BlockEntity>();

        foreach (var pos in occupied)
        {
            foreach ((var dx, var dy) in directions)
            {
                var checkPos = new Vector2I(pos.X + dx, pos.Y + dy);
                (var success, var foundBlock) = board.TryGetBlock(checkPos);

                if (success && foundBlock != centerBlock)
                {
                    if (
                        foundBlock.Type == BlockType.Normal
                        && foundBlock.Color == centerBlock.Color
                    )
                    {
                        sameTypes.Add(foundBlock);
                    }
                    else if (foundBlock.Type == BlockType.Obstacle)
                    {
                        obstacles.Add(foundBlock);
                    }
                }
            }
        }
        return (sameTypes, obstacles);
    }
}

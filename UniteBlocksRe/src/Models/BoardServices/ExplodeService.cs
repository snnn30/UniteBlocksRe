using System.Collections.Generic;
using System.Linq;
using Godot;
using UniteBlocksRe.Models.Block;

namespace UniteBlocksRe.Models.BoardServices;

public static class ExplodeService
{
    public static ExplodeResult Execute(BoardEntity board)
    {
        var steps = new List<ExplodeStep>();
        var exploded = new HashSet<BlockEntity>();

        var currentStepTargets = board
            .Where(bp => bp.Block.Type == BlockType.Bomb)
            .Select(bp => bp.Block)
            .ToHashSet();

        if (currentStepTargets.Count == 0)
        {
            return new ExplodeResult(steps);
        }

        while (currentStepTargets.Count > 0)
        {
            steps.Add(new ExplodeStep(currentStepTargets));
            exploded.UnionWith(currentStepTargets);

            var nextStepTargets = new HashSet<BlockEntity>();

            // 次に誘発されるブロックを探す
            foreach (var target in currentStepTargets)
            {
                foreach (var b in FindInducedBlocks(board, target))
                {
                    if (!exploded.Contains(b))
                    {
                        nextStepTargets.Add(b);
                    }
                }
            }

            // 盤面から今回のステップで爆発したブロックを取り除く
            foreach (var target in currentStepTargets)
            {
                board.Remove(target);
            }

            currentStepTargets = nextStepTargets;
        }

        return new ExplodeResult(steps);
    }

    // 次のステップで誘発されるブロックを探す
    private static BlockEntity[] FindInducedBlocks(BoardEntity board, BlockEntity exploded)
    {
        if (exploded.Type == BlockType.Bomb)
        {
            var targetPos = board.GetPositionOf(exploded) + Vector2I.Down;
            if (!BoardEntity.IsOutOfBounds(targetPos) && board[targetPos] is { } block)
            {
                return [block];
            }
            else
            {
                return [];
            }
        }
        else if (exploded.Type == BlockType.Normal)
        {
            return board
                .GetAdjacentBlocks(exploded)
                .Where(adj =>
                    adj.Type == BlockType.Obstacle
                    || (adj.Type == BlockType.Normal && adj.Color == exploded.Color)
                )
                .ToArray();
        }
        else
        {
            return [];
        }
    }
}

public sealed record ExplodeResult(IReadOnlyList<ExplodeStep> Steps) : IProcessStep
{
    public bool HasExploded => Steps.Count > 0;
}

public sealed record ExplodeStep(IReadOnlySet<BlockEntity> ExplodedBlocks);

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

        // 最初の爆発起点（ボム）を探す
        var initialTargets = board
            .Select(bp => bp.Block)
            .Where(b => b.Type == BlockType.Bomb)
            .ToHashSet();

        if (initialTargets.Count == 0)
        {
            return new ExplodeResult(steps);
        }

        var currentStepTargets = initialTargets; // 現在のステップで爆発する対象
        var exploded = new HashSet<BlockEntity>(); // すでに爆発した（判定済みの）ブロックの座標

        while (currentStepTargets.Count != 0)
        {
            // このステップで爆発するブロックを記録
            steps.Add(new ExplodeStep(currentStepTargets));

            var nextStepTargets = new HashSet<BlockEntity>();

            foreach (var target in currentStepTargets)
            {
                exploded.Add(target);

                // 次のステップで誘発されるブロックを探す
                var induced = FindInducedBlocks(board, target);
                foreach (var b in induced)
                {
                    // すでに爆発済み、または次のステップに登録済みでなければ追加
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

    /// <summary>
    /// 特定のブロックが爆発した際に、次のステップで誘発されるブロックを特定する
    /// </summary>
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

using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.Boards.Operations;

public static class ExplodeService
{
    public static ExplodeResult Execute(Board initialState)
    {
        var steps = new List<ExplodeStep>();
        var currentState = initialState;

        // 最初の爆発起点（ボム）を探す
        var initialTargets = currentState.Grid.Values.Where(b => b is BombBlock).ToHashSet();

        if (initialTargets.Count == 0)
        {
            return new ExplodeResult(initialState, initialState, steps);
        }

        var currentStepTargets = initialTargets; // 現在のステップで爆発する対象
        var exploded = new HashSet<Block>(); // すでに爆発した（判定済みの）ブロックの座標

        while (currentStepTargets.Count != 0)
        {
            // このステップで爆発するブロックを記録
            steps.Add(new ExplodeStep(currentStepTargets));

            var nextStepTargets = new HashSet<Block>();

            foreach (var target in currentStepTargets)
            {
                exploded.Add(target);

                // 次のステップで誘発されるブロックを探す
                var induced = FindInducedBlocks(currentState, target);
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
                currentState = currentState.Remove(target);
            }

            currentStepTargets = nextStepTargets;
        }

        return new ExplodeResult(initialState, currentState, steps);
    }

    /// <summary>
    /// 特定のブロックが爆発した際に、次のステップで誘発されるブロックを特定する
    /// </summary>
    private static IEnumerable<Block> FindInducedBlocks(Board board, Block exploded)
    {
        return exploded switch
        {
            // ボム：１つ下のマスにあるブロックを誘発
            BombBlock => board.Grid.GetValueOrDefault(board.GetPositionOf(exploded) + Vector2I.Down)
                is { } target
                ? [target]
                : [],

            // ノーマル：隣接している同色のブロックを誘発
            NormalBlock normal => board
                .GetAdjacentBlocks(exploded)
                .Where(adj => adj is NormalBlock adjNormal && adjNormal.Color == normal.Color),

            // オブスタクル：何も誘発しない
            _ => [],
        };
    }
}

public sealed record ExplodeResult(Board Before, Board After, IReadOnlyList<ExplodeStep> Steps)
    : BoardOperationStep(Before, After)
{
    public bool HasExploded => Steps.Count > 0;
}

public sealed record ExplodeStep(IReadOnlySet<Block> ExplodedBlocks);

using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.Boards.Operations;

public static class UniteService
{
    public static Vector2I MinUniteSize { get; } = new Vector2I(2, 2);

    public static UniteResult Execute(Board initialState)
    {
        var steps = new List<UniteStep>();
        var currentState = initialState;

        // 盤面を左上から順にスキャン
        for (var y = 0; y < currentState.Bounds.Y; y++)
        {
            for (var x = 0; x < currentState.Bounds.X; x++)
            {
                var currentPos = new Vector2I(x, y);

                // ブロックが存在しない、またはノーマル以外はスキップ
                if (
                    !currentState.Grid.TryGetValue(currentPos, out var startBlock)
                    || startBlock is not NormalBlock startNormal
                )
                {
                    continue;
                }

                // このブロックを起点（左上）として、同じ色が形成する「最大かつ完璧な長方形」の範囲を特定
                var rect = FindPerfectRectangle(currentState, startBlock);

                // 現在のサイズから変化しない場合はスキップ
                if (rect.Size == startBlock.Size)
                {
                    continue;
                }

                var targetBlocks = GetBlocksExclusivelyInArea(currentState, rect)!.ToList();
                var newBlock = new NormalBlock(rect.Size, startNormal.Color);

                // 範囲内のブロックを削除
                foreach (var b in targetBlocks)
                {
                    currentState = currentState.Remove(b);
                }

                // 合体後のブロックを設置
                currentState = currentState.Place(rect.Origin, newBlock);
                steps.Add(new UniteStep(targetBlocks, newBlock));
            }
        }

        return new UniteResult(initialState, currentState, steps);
    }

    /// <summary>
    /// 指定されたブロックを起点に、同色が連続している最大の長方形の範囲を計算する
    /// 最小サイズを考慮する
    /// </summary>
    private static (Vector2I Origin, Vector2I Size) FindPerfectRectangle(
        Board board,
        Block startBlock
    )
    {
        var largestSize = startBlock.Size;
        var origin = board.GetPositionOf(startBlock);
        if (startBlock is not NormalBlock type)
        {
            return (origin, startBlock.Size);
        }
        var color = type.Color;

        // 考え得るサイズを全て考える
        for (var y = startBlock.Size.Y; origin.Y + y <= board.Bounds.Y; y++)
        {
            for (var x = startBlock.Size.X; origin.X + x <= board.Bounds.X; x++)
            {
                if (x * y <= largestSize.Area || x < MinUniteSize.X || y < MinUniteSize.Y)
                {
                    continue;
                }

                // 範囲内が隙間なくはみ出しなく埋まっている事を確認
                var blocks = GetBlocksExclusivelyInArea(board, (origin, new Vector2I(x, y)));
                if (blocks == null)
                {
                    continue;
                }

                // 範囲内のブロックが全て同色であることを確認
                if (blocks.All(b => b is NormalBlock n && n.Color == color))
                {
                    largestSize = new Vector2I(x, y);
                }
            }
        }

        return (origin, largestSize);
    }

    /// <summary>
    /// 指定範囲内に含まれるブロックを収集する。
    /// 隙間がある、あるいははみ出しているブロックが存在する場合はnullを返す
    /// </summary>
    private static IEnumerable<Block>? GetBlocksExclusivelyInArea(
        Board board,
        (Vector2I Origin, Vector2I Size) rect
    )
    {
        var found = new HashSet<Block>();
        for (var y = 0; y < rect.Size.Y; y++)
        {
            for (var x = 0; x < rect.Size.X; x++)
            {
                var pos = rect.Origin + new Vector2I(x, y);

                if (board.Grid.TryGetValue(pos, out var block))
                {
                    var origin = board.GetPositionOf(block);
                    if (!IsWithin(origin, block, rect))
                    {
                        return null; // はみ出していたらアウト
                    }
                    found.Add(block);
                }
                else
                {
                    return null; // 隙間があったらアウト
                }
            }
        }
        return found;
    }

    /// <summary>
    /// rectの中に対象のブロックが収まっているか検証する
    /// </summary>
    private static bool IsWithin(Vector2I origin, Block b, (Vector2I Origin, Vector2I Size) rect) =>
        origin.X >= rect.Origin.X
        && origin.Y >= rect.Origin.Y
        && origin.X + b.Size.X <= rect.Origin.X + rect.Size.X
        && origin.Y + b.Size.Y <= rect.Origin.Y + rect.Size.Y;
}

public sealed record UniteResult(Board Before, Board After, IReadOnlyList<UniteStep> Steps)
    : BoardOperationStep(Before, After)
{
    public bool HasUnited => Steps.Count > 0;
}

public sealed record UniteStep(IReadOnlyList<Block> RemovedBlocks, Block CreatedBlock);

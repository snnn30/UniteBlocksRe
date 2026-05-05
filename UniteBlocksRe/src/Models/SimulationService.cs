using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.OperatingBlocks;

namespace UniteBlocksRe.Models;

public static class SimulationService
{
    /// <summary>
    /// メソッドA: 現在の状態から到達可能な「すべての着地パターン」を列挙する
    /// </summary>
    public static IReadOnlyList<SimulationResult> SimulateAll(
        OperatingBlocksEntity initial,
        BoardEntity board
    )
    {
        // キー: (最終ParentPos, 最終ChildPos)
        var bestResults = new Dictionary<(Vector2I, Vector2I), SimulationResult>();

        // 空中探索の訪問済み管理 (座標 + IsHalfUp)
        // 手順を考慮しないため、同じ地点に「より低コスト」で辿り着くことだけを保証
        var minCosts = new Dictionary<(Vector2I P, Vector2I C, bool Half), int>();
        var pq = new PriorityQueue<OperatingBlocksEntity, int>();

        var start = initial.Clone();
        pq.Enqueue(start, 0);
        minCosts[(start.ParentPos, start.ChildPos, start.IsHalfUp)] = 0;

        while (pq.Count > 0)
        {
            pq.TryDequeue(out var current, out var currentCost);
            var stateKey = (current.ParentPos, current.ChildPos, current.IsHalfUp);

            if (minCosts.TryGetValue(stateKey, out var best) && currentCost > best)
            {
                continue;
            }

            // --- 着地パターンの記録 ---
            var landingKey = GetLandingKey(current);
            if (!bestResults.ContainsKey(landingKey))
            {
                bestResults[landingKey] = Finalize(current, board);
            }

            // --- 次の操作（1ポチ分）を探索 ---
            foreach (var next in GetPossibleNextStates(current))
            {
                var nextKey = (next.ParentPos, next.ChildPos, next.IsHalfUp);
                var nextCost = currentCost + 1; // 操作回数

                if (!minCosts.TryGetValue(nextKey, out var b) || nextCost < b)
                {
                    minCosts[nextKey] = nextCost;
                    pq.Enqueue(next, nextCost);
                }
            }
        }

        return [.. bestResults.Values];
    }

    private static IEnumerable<OperatingBlocksEntity> GetPossibleNextStates(
        OperatingBlocksEntity current
    )
    {
        // 左右移動
        foreach (var dir in new[] { MoveDirection.Left, MoveDirection.Right })
        {
            var next = current.Clone();
            while (next.TryMove(dir))
            {
                yield return next.Clone();
            }
        }

        // 回転（1〜3回）
        foreach (var dir in new[] { RotateDirection.CW, RotateDirection.ACW })
        {
            var next = current.Clone();
            for (var i = 0; i < 3; i++)
            {
                if (!next.TryRotate(dir).Success)
                {
                    break;
                }

                yield return next.Clone();
            }
        }

        // 落下
        var down = current.Clone();
        while (down.TryDrop())
        {
            yield return down.Clone();
        }
    }

    private static (Vector2I P, Vector2I C) GetLandingKey(OperatingBlocksEntity state)
    {
        var temp = state.Clone();
        while (temp.TryDrop()) { }
        return (temp.ParentPos, temp.ChildPos);
    }

    private static SimulationResult Finalize(OperatingBlocksEntity state, BoardEntity board)
    {
        var simBoard = board.Clone();
        var moving = state.Clone();
        while (moving.TryDrop()) { } // 完全に落とし切る

        simBoard.Place(moving.ParentPos, moving.Parent);
        if (moving.Child is { } child)
        {
            simBoard.Place(moving.ChildPos, child);
        }

        // BoardService.Process で連鎖・消去をシミュレート
        var processResult = BoardService.Process(simBoard);

        return new SimulationResult(simBoard, processResult, moving.ParentPos, moving.ChildPos);
    }
}

public sealed record SimulationResult(
    BoardEntity Board,
    ProcessResult BoardOperations,
    Vector2I ParentDestination,
    Vector2I ChildDestination
);

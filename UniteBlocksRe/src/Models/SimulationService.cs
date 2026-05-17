using System.Collections.Generic;
using System.Linq;
using Godot;
using UniteBlocksRe.Models.OperatingBlocks;

namespace UniteBlocksRe.Models;

public static class SimulationService
{
    /// <summary>
    /// 操作フローを「横移動 -> 回転 -> 落下」に限定して、到達可能な最終地点をすべて列挙する
    /// </summary>
    public static IReadOnlyList<SimulationResult> EnumerateAllDestinations(
        OperatingBlocksEntity initial,
        BoardEntity board
    )
    {
        var destinations = new Dictionary<(Vector2I, Vector2I), SimulationResult>();

        // 横移動を全パターン試行
        foreach (var moved in GetHorizontalPatterns(initial))
        {
            // 移動後の各状態から、全回転パターンを試行
            foreach (var rotated in GetRotationPatterns(moved.Entity))
            {
                // 回転後の各状態から、一番下まで一気に落とす
                var finalEntity = rotated.Entity.Clone();
                var dropCount = 0;
                while (finalEntity.TryDrop())
                {
                    dropCount++;
                }

                // 最終座標をキーにする
                var key = (finalEntity.ParentPos, finalEntity.ChildPos);

                // 手順を構築 (移動 -> 回転 -> 落下)
                var steps = new List<StepInfo>();
                if (moved.Step != null)
                {
                    steps.Add(moved.Step);
                }

                if (rotated.Step != null)
                {
                    steps.Add(rotated.Step);
                }

                if (dropCount > 0)
                {
                    steps.Add(new StepInfo(new DropOperation(), dropCount));
                }

                // まだ見つかっていない場所、またはより手順の短い(Step数が少ない)ものを採用
                if (
                    !destinations.TryGetValue(key, out var existing)
                    || steps.Count < existing.Steps.Count
                )
                {
                    destinations[key] = CreateResult(finalEntity, board, steps);
                }
            }
        }

        return destinations.Values.ToList();
    }

    /// <summary>
    /// 初期位置から左右に行ける全パターンを列挙（0移動含む）
    /// </summary>
    private static IEnumerable<(
        OperatingBlocksEntity Entity,
        StepInfo? Step
    )> GetHorizontalPatterns(OperatingBlocksEntity initial)
    {
        // 移動なし
        yield return (initial.Clone(), null);

        foreach (var dir in new[] { MoveDirection.Left, MoveDirection.Right })
        {
            var current = initial.Clone();
            var count = 0;
            while (current.TryMove(dir))
            {
                count++;
                yield return (current.Clone(), new StepInfo(new MoveOperation(dir), count));
            }
        }
    }

    /// <summary>
    /// 特定の座標で可能な全回転パターンを列挙
    /// </summary>
    private static IEnumerable<(OperatingBlocksEntity Entity, StepInfo? Step)> GetRotationPatterns(
        OperatingBlocksEntity moved
    )
    {
        // 回転なし
        yield return (moved.Clone(), null);

        for (var i = 1; i <= 3; i++)
        {
            foreach (var dir in new[] { RotateDirection.CW, RotateDirection.ACW })
            {
                var current = moved.Clone();
                var possible = true;

                for (var j = 0; j < i; j++)
                {
                    if (!current.TryRotate(dir).Success)
                    {
                        possible = false;
                        break;
                    }
                }

                if (possible)
                {
                    yield return (current.Clone(), new StepInfo(new RotateOperation(dir), i));
                }
            }
        }
    }

    private static SimulationResult CreateResult(
        OperatingBlocksEntity state,
        BoardEntity board,
        IReadOnlyList<StepInfo> steps
    )
    {
        var simBoard = board.Clone();
        simBoard.Place(state.ParentPos, state.Parent);
        if (state.Child is { } child)
        {
            simBoard.Place(state.ChildPos, child);
        }

        return new SimulationResult(
            simBoard,
            BoardService.Process(simBoard),
            state.ParentPos,
            state.ChildPos,
            steps
        );
    }
}

public sealed record SimulationResult(
    BoardEntity Board,
    ProcessResult BoardOperations,
    Vector2I ParentDestination,
    Vector2I ChildDestination,
    IReadOnlyList<StepInfo> Steps
);

public record StepInfo(BlockOperationStep Operation, int Count);

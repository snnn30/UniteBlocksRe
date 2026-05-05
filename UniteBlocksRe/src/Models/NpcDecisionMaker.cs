using System.Collections.Generic;
using System.Linq;
using UniteBlocksRe.src.Models.Evaluation;
using UniteBlocksRe.src.Models.OperatingBlocks;

namespace UniteBlocksRe.src.Models;

public class NpcDecisionMaker
{
    private readonly EvaluationWeight _evaluationWeight;

    public NpcDecisionMaker(EvaluationWeight evaluationWeight)
    {
        _evaluationWeight = evaluationWeight;
    }

    /// <summary>
    /// 現在の状態から次にとるべき1操作を決定する。
    /// </summary>
    public BlockOperationStep GetNextStep(
        OperatingBlocksEntity operating,
        BoardEntity board,
        BlockOperationStep lastStep
    )
    {
        // 最良の目的地を計算
        var (_, destination) = EvaluationService.UpdateDestination(
            operating,
            board,
            _evaluationWeight
        );
        if (destination == null)
        {
            return new StuckOperation();
        }

        // 現在地から実行可能な1回の操作をすべて列挙
        var candidates = GetImmediateMoves(operating);

        // 目的地に辿り着けるルートが残る操作だけに絞り込む
        var validMoves = candidates.Where(c => CanReach(c.NextState, board, destination)).ToList();

        // 操作不能な場合
        if (validMoves.Count == 0)
        {
            return new StuckOperation();
        }

        // 前の手と同じ操作を優先する
        return validMoves
            .OrderByDescending(c => IsSameType(c.Step, lastStep))
            .ThenByDescending(c => GetStepPriority(c.Step))
            .First()
            .Step;
    }

    /// <summary>
    /// 現在の状態から一回の操作で遷移できる状態を列挙する。
    /// </summary>
    private static IReadOnlyList<(
        BlockOperationStep Step,
        OperatingBlocksEntity NextState
    )> GetImmediateMoves(OperatingBlocksEntity current)
    {
        var results = new List<(BlockOperationStep, OperatingBlocksEntity)>();

        // 左右移動
        foreach (var dir in new[] { MoveDirection.Left, MoveDirection.Right })
        {
            var next = current.Clone();
            if (next.TryMove(dir))
            {
                results.Add((new MoveOperation(dir), next));
            }
        }

        // 回転
        foreach (var dir in new[] { RotateDirection.CW, RotateDirection.ACW })
        {
            var next = current.Clone();
            if (next.TryRotate(dir).Success)
            {
                results.Add((new RotateOperation(dir), next));
            }
        }

        // 落下
        var down = current.Clone();
        if (down.TryDrop())
        {
            results.Add((new DropOperation(), down));
        }

        return results;
    }

    /// <summary>
    /// 指定された状態から、目的地に到達できるかをシミュレーションで判定する。
    /// </summary>
    private static bool CanReach(
        OperatingBlocksEntity start,
        BoardEntity board,
        SimulationResult target
    )
    {
        var reachableLandings = SimulationService.SimulateAll(start, board);

        return reachableLandings.Any(r =>
            r.ParentDestination == target.ParentDestination
            && r.ChildDestination == target.ChildDestination
        );
    }

    private static bool IsSameType(BlockOperationStep next, BlockOperationStep last)
    {
        return last != null && next != null && next.GetType() == last.GetType();
    }

    /// <summary>
    /// 挙動安定化のための操作自体の優先順位。
    /// </summary>
    private static int GetStepPriority(BlockOperationStep step)
    {
        return step switch
        {
            MoveOperation => 3,
            RotateOperation => 2,
            DropOperation => 1,
            _ => 0,
        };
    }
}

public abstract record BlockOperationStep;

public record MoveOperation(MoveDirection Direction) : BlockOperationStep;

public record RotateOperation(RotateDirection Direction) : BlockOperationStep;

public record DropOperation : BlockOperationStep;

public record StuckOperation : BlockOperationStep;

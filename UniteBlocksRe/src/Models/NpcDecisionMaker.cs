using UniteBlocksRe.Models.Evaluation;
using UniteBlocksRe.Models.OperatingBlocks;

namespace UniteBlocksRe.Models;

public class NpcDecisionMaker
{
    private readonly EvaluationWeight _evaluationWeight;

    private SimulationResult _lastDestination;

    public NpcDecisionMaker(EvaluationWeight evaluationWeight)
    {
        _evaluationWeight = evaluationWeight;
    }

    // <summary>
    /// 最良の目的地と、そこに至るまでの全ステップを返す
    /// </summary>
    public SimulationResult GetBestDestination(OperatingBlocksEntity operating, BoardEntity board)
    {
        // 目的地がない、あるいは状況が変わった場合のみ重い計算を行う
        var (_, destination) = EvaluationService.UpdateDestination(
            operating,
            board,
            _evaluationWeight,
            _lastDestination // 前回の目的地を渡して安定性を確保
        );

        _lastDestination = destination;

        if (destination.Steps.Count == 0)
        {
            return destination with { Steps = [new(new StuckOperation(), 0)] };
        }

        return destination;
    }

    public bool ShouldUseBomb(BoardEntity board, BlockEntity parent, BlockEntity child)
    {
        var (canSpawn1, blocks) = OperatingBlocksEntity.TrySpawnDouble(parent, child, board);
        var (canSpawn2, bomb) = OperatingBlocksEntity.TrySpawnSingle(
            BlockEntity.CreateBomb(),
            board
        );

        if (!canSpawn1 || !canSpawn2)
        {
            return false;
        }

        var (blockResult, _) = EvaluationService.UpdateDestination(
            blocks,
            board,
            _evaluationWeight,
            null
        );
        var (bombResult, _) = EvaluationService.UpdateDestination(
            bomb,
            board,
            _evaluationWeight,
            null
        );

        return bombResult.TotalScore > blockResult.TotalScore;
    }
}

public abstract record BlockOperationStep;

public record MoveOperation(MoveDirection Direction) : BlockOperationStep;

public record RotateOperation(RotateDirection Direction) : BlockOperationStep;

public record DropOperation : BlockOperationStep;

public record StuckOperation : BlockOperationStep;

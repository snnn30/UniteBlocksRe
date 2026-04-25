using System.Linq;
using UniteBlocksRe.src.Models.Services;
using UniteBlocksRe.src.Models.ValueObjects.Simulation;

namespace UniteBlocksRe.src.Models.Entities;

public class ActionSelector
{
    private readonly BoardEvaluationWeights _boardWeights;
    private ExplodeEvaluationWeights _explosionWeights;

    public ActionSelector(
        BoardEvaluationWeights boardWeights,
        ExplodeEvaluationWeights explodeEvaluationWeights
    )
    {
        _boardWeights = boardWeights;
        _explosionWeights = explodeEvaluationWeights;
    }

    public (
        BoardEntity SimulatedBoard,
        BoardEvaluationResult EvaluationResult,
        OperationInstructions Instructions
    ) GetBestMoveBlock(OperatingBlocksEntity operating)
    {
        var simResult = Simlation.SimulateSetBlocks(operating);

        var bestResult = simResult
            .Results.Select(res => new
            {
                res.Board,
                Evaluation = Evaluation.BoardEvaluate(res.Board, _boardWeights),
                res.Operations,
            })
            .OrderByDescending(x => x.Evaluation.TotalScore)
            .First();

        return (bestResult.Board, bestResult.Evaluation, bestResult.Operations);
    }

    public (
        BoardEntity SimulatedBoard,
        BoardEvaluationResult BoardEvaluationResult,
        ExplodeEvaluationResult ExplodeEvaluationResult,
        OperationInstructions Instructions
    ) GetBestMoveBomb(OperatingBlocksEntity operating)
    {
        var simResult = Simlation.SimulateSetBomb(operating);

        var bestResult = simResult
            .Results.Select(res => new
            {
                res.Board,
                BoardEvaluationResult = Evaluation.BoardEvaluate(res.Board, _boardWeights),
                ExplodeEvaluationResult = Evaluation.ExplodeEvaluate(
                    res.Explode,
                    _explosionWeights
                ),
                res.Operations,
            })
            .OrderByDescending(x =>
                x.BoardEvaluationResult.TotalScore + x.ExplodeEvaluationResult.Score
            )
            .First();

        return (
            bestResult.Board,
            bestResult.BoardEvaluationResult,
            bestResult.ExplodeEvaluationResult,
            bestResult.Operations
        );
    }
}

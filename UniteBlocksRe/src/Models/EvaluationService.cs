using System;
using System.Linq;
using UniteBlocksRe.src.Models.Evaluation;

namespace UniteBlocksRe.src.Models;

public static class EvaluationService
{
    public static (EvaluationResult evaluation, SimulationResult simulation) UpdateDestination(
        OperatingBlocksEntity operating,
        BoardEntity board,
        EvaluationWeight weight
    )
    {
        ValidateWeights(weight);

        var allSimResults = SimulationService.SimulateAll(operating, board);
        var bestPlan = allSimResults
            .Select(sim => (Evaluation: Evaluate(sim, weight), Simulation: sim))
            .MaxBy(plan => plan.Evaluation.TotalScore);
        return bestPlan;
    }

    private static EvaluationResult Evaluate(
        SimulationResult simulationResult,
        EvaluationWeight weight
    )
    {
        var scores = EvaluationCriterion
            .GetAll()
            .ToDictionary(c => c, c => c.CalculateScore(weight.Weights[c], simulationResult));

        return new EvaluationResult(scores);
    }

    private static void ValidateWeights(EvaluationWeight weight)
    {
        var allCriteria = EvaluationCriterion.GetAll();
        var missing = allCriteria
            .Where(c => !weight.Weights.ContainsKey(c))
            .Select(c => c.Name)
            .ToArray();

        if (missing.Length != 0)
        {
            throw new InvalidOperationException(
                $"評価の重みが不足している　追加が必要な項目: {string.Join(", ", missing)}"
            );
        }
    }
}

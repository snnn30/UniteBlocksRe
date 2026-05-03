using System.Collections.Immutable;
using UniteBlocksRe.Domain.Evaluations;
using UniteBlocksRe.Domain.Simulations;

namespace UniteBlocksRe.Domain;

public class EvaluationService
{
    private readonly EvaluationWeight _weight;

    public EvaluationService(EvaluationWeight weight)
    {
        ValidateWeights(weight);
        _weight = weight;
    }

    public (EvaluationResult evaluation, SimulationResult simulation) UpdatePlan(
        OperatingBlockPair operating,
        Board board
    )
    {
        var allSimResults = SimulationService.SimulateAll(operating, board);
        var bestPlan = allSimResults
            .Select(sim => (Evaluation: Evaluate(sim), Simulation: sim))
            .MaxBy(plan => plan.Evaluation.TotalScore);
        return bestPlan;
    }

    public EvaluationResult Evaluate(SimulationResult simulationResult)
    {
        var scores = EvaluationCriterion.All.ToImmutableDictionary(
            c => c,
            c => c.CalculateScore(_weight.Weights[c], simulationResult)
        );

        return new EvaluationResult(scores);
    }

    private void ValidateWeights(EvaluationWeight weight)
    {
        var allCriteria = EvaluationCriterion.All.ToArray();
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

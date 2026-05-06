using System;
using System.Linq;
using UniteBlocksRe.Models.Evaluation;

namespace UniteBlocksRe.Models;

public static class EvaluationService
{
    public static (EvaluationResult evaluation, SimulationResult simulation) UpdateDestination(
        OperatingBlocksEntity operating,
        BoardEntity board,
        EvaluationWeight weight,
        SimulationResult lastDestination
    )
    {
        ValidateWeights(weight);

        // 1. 移動・回転・落下の順で全パターンを列挙
        // ここで返される各 SimulationResult.Steps には、既に綺麗な手順が入っている
        var destinations = SimulationService.EnumerateAllDestinations(operating, board);

        // 2. 盤面評価で最高の目的地を選ぶ
        // MaxBy の第2引数で、スコアが同点なら「前回の目的地と同じ場所」を優先して操作のガタつきを防ぐ
        var best = destinations
            .Select(sim => (Eval: Evaluate(sim, weight), Sim: sim))
            .MaxBy(x => (x.Eval.TotalScore, IsSameDestination(x.Sim, lastDestination)));

        return (best.Eval, best.Sim);
    }

    public static EvaluationResult Evaluate(
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

    private static bool IsSameDestination(SimulationResult current, SimulationResult last)
    {
        if (last == null)
        {
            return false;
        }
        return current.ParentDestination == last.ParentDestination
            && current.ChildDestination == last.ChildDestination;
    }
}

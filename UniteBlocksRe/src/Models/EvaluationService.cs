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
        SimulationResult? lastDestination
    )
    {
        var destinations = SimulationService.EnumerateAllDestinations(operating, board);

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
        var scores = Enum.GetValues<EvaluationCriterion>()
            .ToDictionary(c => c, c => c.CalculateScore(weight.GetWeight(c), simulationResult));
        return new EvaluationResult(scores);
    }

    private static bool IsSameDestination(SimulationResult current, SimulationResult? last)
    {
        if (last == null)
        {
            return false;
        }
        return current.ParentDestination == last.ParentDestination
            && current.ChildDestination == last.ChildDestination;
    }
}

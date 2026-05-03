using System.Collections.Immutable;

namespace UniteBlocksRe.Domain.Evaluations;

public record EvaluationResult(ImmutableDictionary<EvaluationCriterion, int> Scores)
{
    public int TotalScore => Scores.Values.Sum();
}

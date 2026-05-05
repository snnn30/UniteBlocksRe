using System.Collections.Generic;
using System.Linq;

namespace UniteBlocksRe.Models.Evaluation;

public record EvaluationResult(IReadOnlyDictionary<EvaluationCriterion, int> Scores)
{
    public int TotalScore => Scores.Values.Sum();
}

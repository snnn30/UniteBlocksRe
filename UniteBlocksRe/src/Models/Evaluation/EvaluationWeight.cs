using System.Collections.Generic;

namespace UniteBlocksRe.Models.Evaluation;

public abstract record EvaluationWeight
{
    public abstract IReadOnlyDictionary<EvaluationCriterion, int> Weights { get; }
}

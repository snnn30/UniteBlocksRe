using System.Collections.Generic;

namespace UniteBlocksRe.src.Models.Evaluation;

public abstract record EvaluationWeight
{
    public abstract IReadOnlyDictionary<EvaluationCriterion, int> Weights { get; }
}

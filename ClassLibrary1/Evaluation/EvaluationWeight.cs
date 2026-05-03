using System.Collections.Immutable;

namespace UniteBlocksRe.Domain.Evaluations;

public abstract record EvaluationWeight
{
    public abstract ImmutableDictionary<EvaluationCriterion, int> Weights { get; }
}

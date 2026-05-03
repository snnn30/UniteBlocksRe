using System.Collections.Immutable;

namespace UniteBlocksRe.Domain.Evaluations.EvaluationWeights;

public sealed record DefaultEvaluationWeight : EvaluationWeight
{
    public override ImmutableDictionary<EvaluationCriterion, int> Weights => _weights;
    private readonly ImmutableDictionary<EvaluationCriterion, int> _weights = new Dictionary<
        EvaluationCriterion,
        int
    >
    {
        { EvaluationCriterion.BlockSizeBonus, 10 },
        { EvaluationCriterion.SameColorAdjacentBonus, 10 },
        { EvaluationCriterion.ExplodeBlockBonus, 12 },
        { EvaluationCriterion.HeightPenalty, -2 },
        { EvaluationCriterion.ObstaclePenalty, -20 },
        { EvaluationCriterion.DifferentColorAdjacentPenalty, -4 },
        { EvaluationCriterion.UseBombPenalty, -80 },
        { EvaluationCriterion.CantSpawnPenalty, -100000 },
    }.ToImmutableDictionary();
}

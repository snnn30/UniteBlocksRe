using System.Collections.Generic;

namespace UniteBlocksRe.Models.Evaluation.EvaluationWeights;

public sealed record DefaultEvaluationWeight : EvaluationWeight
{
    public override IReadOnlyDictionary<EvaluationCriterion, int> Weights => _weights;
    private readonly IReadOnlyDictionary<EvaluationCriterion, int> _weights = new Dictionary<
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
    };
}

using System;

namespace UniteBlocksRe.Models.Evaluation.EvaluationWeights;

public sealed record DefaultEvaluationWeight : EvaluationWeight
{
    public override int GetWeight(EvaluationCriterion criterion)
    {
        return criterion switch
        {
            EvaluationCriterion.BlockSizeBonus => 10,
            EvaluationCriterion.SameColorAdjacentBonus => 10,
            EvaluationCriterion.ExplodeBlockBonus => 12,
            EvaluationCriterion.HeightPenalty => -2,
            EvaluationCriterion.ObstaclePenalty => -20,
            EvaluationCriterion.DifferentColorAdjacentPenalty => -8,
            EvaluationCriterion.UseBombPenalty => -80,
            EvaluationCriterion.CantSpawnPenalty => -100000,
            _ => throw new ArgumentOutOfRangeException(
                nameof(criterion),
                criterion,
                "未定義の評価基準"
            ),
        };
    }
}

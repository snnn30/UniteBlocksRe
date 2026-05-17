namespace UniteBlocksRe.Models.Evaluation;

public abstract record EvaluationWeight
{
    public abstract int GetWeight(EvaluationCriterion criterion);
}

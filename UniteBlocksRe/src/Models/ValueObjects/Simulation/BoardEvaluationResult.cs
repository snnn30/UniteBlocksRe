namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public record struct BoardEvaluationResult
{
    public float BlockSizeScore { get; set; }
    public float SameAdjacentScore { get; set; }

    public float HeightPenaltyScore { get; set; }
    public float ObstaclePenaltyScore { get; set; }
    public float DifferentAdjacentScore { get; set; }
    public float CantSpawnPenaltyScore { get; set; }

    public float TotalScore =>
        BlockSizeScore
        + SameAdjacentScore
        + HeightPenaltyScore
        + ObstaclePenaltyScore
        + DifferentAdjacentScore
        + CantSpawnPenaltyScore;
}

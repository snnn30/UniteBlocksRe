namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public record struct BoardEvaluationResult
{
    public float BlockSizeScore { get; set; }
    public float AdjacentScore { get; set; }

    public float HeightPenaltyScore { get; set; }
    public float ObstaclePenaltyScore { get; set; }
    public float CantSpawnPenaltyScore { get; set; }

    public float TotalScore =>
        BlockSizeScore
        + AdjacentScore
        + HeightPenaltyScore
        + ObstaclePenaltyScore
        + CantSpawnPenaltyScore;

    public override string ToString() =>
        $"Total: {TotalScore:F1} (Size: {BlockSizeScore:F1}, Adj: {AdjacentScore:F1}, H: {HeightPenaltyScore:F1}, Obs: {ObstaclePenaltyScore:F1}, Spawn: {CantSpawnPenaltyScore:F1})";
}

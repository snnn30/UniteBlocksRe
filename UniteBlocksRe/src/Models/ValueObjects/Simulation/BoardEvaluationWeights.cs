namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public record struct BoardEvaluationWeights
{
    public required float BlockSizeWeight { get; init; }
    public required float SameColorAdjacentWeight { get; init; }

    public required float HeightPenalty { get; init; }
    public required float ObstaclePenalty { get; init; }
    public required float DifferentColorAdjacentPenalty { get; init; }
}

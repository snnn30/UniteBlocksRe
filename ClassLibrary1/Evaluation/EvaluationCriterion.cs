using UniteBlocksRe.Domain.Boards.Operations;
using UniteBlocksRe.Domain.Common;
using UniteBlocksRe.Domain.Simulations;

namespace UniteBlocksRe.Domain.Evaluations;

public class EvaluationCriterion : Enumeration<EvaluationCriterion>
{
    public static readonly EvaluationCriterion BlockSizeBonus = new(
        0,
        nameof(BlockSizeBonus),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var block in sim.FinalState.Grid.Values)
            {
                var area = block.Size.Area;
                score += area * area * weight;
            }
            return score;
        }
    );

    public static readonly EvaluationCriterion SameColorAdjacentBonus = new(
        1,
        nameof(SameColorAdjacentBonus),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var block in sim.FinalState.Grid.Values)
            {
                var adjacents = sim.FinalState.GetAdjacentBlocks(block);
                foreach (var a in adjacents)
                {
                    if (block is NormalBlock n1 && a is NormalBlock n2 && n1.Color == n2.Color)
                    {
                        score += weight;
                    }
                }
            }
            return score / 2;
        }
    );

    public static readonly EvaluationCriterion ExplodeBlockBonus = new(
        2,
        nameof(ExplodeBlockBonus),
        (weight, sim) =>
        {
            var score = 0;
            var explodeResult = sim.BoardOperations.OfType<ExplodeResult>().FirstOrDefault();
            if (explodeResult == null)
            {
                return score;
            }
            foreach (var step in explodeResult.Steps)
            {
                foreach (var block in step.ExplodedBlocks)
                {
                    var area = block.Size.Area;
                    score += area * area * weight;
                }
            }
            return score;
        }
    );

    public static readonly EvaluationCriterion HeightPenalty = new(
        3,
        nameof(HeightPenalty),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var pos in sim.FinalState.Grid.Keys)
            {
                score += (sim.FinalState.Bounds.Y - 1 - pos.Y) * weight;
            }
            return score;
        }
    );

    public static readonly EvaluationCriterion ObstaclePenalty = new(
        4,
        nameof(ObstaclePenalty),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var block in sim.FinalState.Grid.Values)
            {
                if (block is ObstacleBlock)
                {
                    score += weight;
                }
            }
            return score;
        }
    );

    public static readonly EvaluationCriterion DifferentColorAdjacentPenalty = new(
        5,
        nameof(DifferentColorAdjacentPenalty),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var block in sim.FinalState.Grid.Values)
            {
                var adjacents = sim.FinalState.GetAdjacentBlocks(block);
                foreach (var a in adjacents)
                {
                    if (block is NormalBlock n1 && a is NormalBlock n2 && n1.Color != n2.Color)
                    {
                        score += weight;
                    }
                }
            }
            return score / 2;
        }
    );

    public static readonly EvaluationCriterion UseBombPenalty = new(
        6,
        nameof(UseBombPenalty),
        (weight, sim) =>
        {
            var explodeResult = sim.BoardOperations.OfType<ExplodeResult>().FirstOrDefault();
            if (explodeResult == null)
            {
                return 0;
            }
            else
            {
                return weight;
            }
        }
    );

    public static readonly EvaluationCriterion CantSpawnPenalty = new(
        7,
        nameof(CantSpawnPenalty),
        (weight, sim) =>
        {
            var finalState = sim.FinalState;
            if (finalState.Grid.ContainsKey(finalState.SpawnPos))
            {
                return weight;
            }
            else
            {
                return 0;
            }
        }
    );

    private EvaluationCriterion(
        int id,
        string name,
        Func<int, SimulationResult, int> scoreCalculator
    )
        : base(id, name)
    {
        _scoreCalculator = scoreCalculator;
    }

    private readonly Func<int, SimulationResult, int> _scoreCalculator;

    public int CalculateScore(int weight, SimulationResult simResult) =>
        _scoreCalculator(weight, simResult);
}

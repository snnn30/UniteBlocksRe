using System;
using System.Linq;
using UniteBlocksRe.Domain.Common;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.Block;
using UniteBlocksRe.src.Models.BoardServices;

namespace UniteBlocksRe.src.Models.Evaluation;

public class EvaluationCriterion : Enumeration<EvaluationCriterion>
{
    public static readonly EvaluationCriterion BlockSizeBonus = new(
        0,
        nameof(BlockSizeBonus),
        (weight, sim) =>
        {
            var score = 0;
            foreach (var (block, pos) in sim.Board)
            {
                var area = block.Size.GetArea();
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
            foreach (var (block, pos) in sim.Board)
            {
                var adjacents = sim.Board.GetAdjacentBlocks(block);
                foreach (var a in adjacents)
                {
                    if (
                        block.Type == BlockType.Normal
                        && a.Type == BlockType.Normal
                        && block.Color == a.Color
                    )
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
            var explodeResult = sim.BoardOperations.Steps.OfType<ExplodeResult>().FirstOrDefault();
            if (explodeResult == null)
            {
                return score;
            }
            foreach (var step in explodeResult.Steps)
            {
                foreach (var block in step.ExplodedBlocks)
                {
                    var area = block.Size.GetArea();
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
            foreach (var (block, pos) in sim.Board)
            {
                score += (BoardEntity.Size.Y - 1 - pos.Y) * weight;
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
            foreach (var (block, pos) in sim.Board)
            {
                if (block.Type == BlockType.Obstacle)
                {
                    score += weight * block.Size.GetArea();
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
            foreach (var (block, pos) in sim.Board)
            {
                var adjacents = sim.Board.GetAdjacentBlocks(block);
                foreach (var a in adjacents)
                {
                    if (
                        block.Type == BlockType.Normal
                        && a.Type == BlockType.Normal
                        && block.Color != a.Color
                    )
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
            var explodeResult = sim.BoardOperations.Steps.OfType<ExplodeResult>().FirstOrDefault();
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
            if (sim.Board.Select(bp => bp.Pos).Contains(BoardEntity.SpawnPosition))
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

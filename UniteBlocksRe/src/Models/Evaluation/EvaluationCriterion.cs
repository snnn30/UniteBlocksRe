using System;
using System.Linq;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Models.Evaluation;

public enum EvaluationCriterion
{
    BlockSizeBonus,
    SameColorAdjacentBonus,
    ExplodeBlockBonus,
    HeightPenalty,
    ObstaclePenalty,
    DifferentColorAdjacentPenalty,
    UseBombPenalty,
    CantSpawnPenalty,
}

public static class EvaluationCriterionExtensions
{
    public static int CalculateScore(
        this EvaluationCriterion criterion,
        int weight,
        SimulationResult sim
    )
    {
        return criterion switch
        {
            EvaluationCriterion.BlockSizeBonus => CalcBlockSize(weight, sim),
            EvaluationCriterion.SameColorAdjacentBonus => CalcSameColor(weight, sim),
            EvaluationCriterion.ExplodeBlockBonus => CalcExplode(weight, sim),
            EvaluationCriterion.HeightPenalty => CalcHeight(weight, sim),
            EvaluationCriterion.ObstaclePenalty => CalcObstacle(weight, sim),
            EvaluationCriterion.DifferentColorAdjacentPenalty => CalcDiffColor(weight, sim),
            EvaluationCriterion.UseBombPenalty => CalcUseBomb(weight, sim),
            EvaluationCriterion.CantSpawnPenalty => CalcCantSpawn(weight, sim),
            _ => throw new ArgumentOutOfRangeException(
                nameof(criterion),
                criterion,
                "未定義の評価基準"
            ),
        };
    }

    private static int CalcBlockSize(int weight, SimulationResult sim)
    {
        var score = 0;
        foreach (var (block, pos) in sim.Board)
        {
            var area = block.Size.GetArea();
            score += area * area * weight;
        }
        return score;
    }

    private static int CalcSameColor(int weight, SimulationResult sim)
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

    private static int CalcExplode(int weight, SimulationResult sim)
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

    private static int CalcHeight(int weight, SimulationResult sim)
    {
        var score = 0;
        foreach (var (block, pos) in sim.Board)
        {
            score += (BoardEntity.Size.Y - 1 - pos.Y) * weight;
        }
        return score;
    }

    private static int CalcObstacle(int weight, SimulationResult sim)
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

    private static int CalcDiffColor(int weight, SimulationResult sim)
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

    private static int CalcUseBomb(int weight, SimulationResult sim)
    {
        var explodeResult = sim.BoardOperations.Steps.OfType<ExplodeResult>().FirstOrDefault();
        return explodeResult == null ? 0 : weight;
    }

    private static int CalcCantSpawn(int weight, SimulationResult sim)
    {
        return sim.Board.Select(bp => bp.Pos).Contains(BoardEntity.SpawnPosition) ? weight : 0;
    }
}

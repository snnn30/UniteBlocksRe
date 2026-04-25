using System.Linq;
using Godot;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;
using UniteBlocksRe.src.Models.ValueObjects.Simulation;

namespace UniteBlocksRe.src.Models.Services;

public static class Evaluation
{
    public static ExplodeEvaluationResult ExplodeEvaluate(
        ExplodeResult explodeResult,
        ExplodeEvaluationWeights weights
    )
    {
        var score = 0f;
        foreach (var step in explodeResult.Steps)
        {
            foreach (var block in step.Exploded)
            {
                var area = block.Size.GetArea();
                score += area * area * weights.Weight;
            }
        }
        return new ExplodeEvaluationResult(score);
    }

    public static BoardEvaluationResult BoardEvaluate(
        BoardEntity board,
        BoardEvaluationWeights weights
    )
    {
        BoardEvaluationResult result = new();

        if (!board.CanPlace(BoardEntity.SpawnPosition, Vector2I.One))
        {
            result.CantSpawnPenaltyScore = -10000f;
            return result;
        }

        var blocks = board.GetAllBlocks().ToArray();

        // サイズに基づくスコア
        foreach (var block in blocks)
        {
            var area = block.Size.GetArea();
            result.BlockSizeScore += area * area * weights.BlockSizeWeight;
        }

        // 同色隣接数に基づくスコア
        foreach (var block in blocks)
        {
            var adjacents = board.GetAdjacentBlocks(block);
            foreach (var a in adjacents)
            {
                if (a.Type == block.Type)
                {
                    result.AdjacentScore += weights.SameColorAdjacentWeight;
                }
            }
        }

        // 高さに基づくスコア
        // 修正が必要　同じだけのエリアをとっていても大きなブロック１つと複数の小さなブロックでスコアが大きく変わる
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                if (!board.CanPlace(new(x, y), Vector2I.One))
                {
                    var score = (BoardEntity.Size.Y - 1 - y) * weights.HeightPenalty;
                    result.HeightPenaltyScore += score;
                }
            }
        }

        // 障害物の数に基づくスコア
        foreach (var block in blocks)
        {
            if (block.Type == ValueObjects.BlockType.Obstacle)
            {
                result.ObstaclePenaltyScore += weights.ObstaclePenalty;
            }
        }

        return result;
    }
}

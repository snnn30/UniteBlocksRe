using System.Collections.Generic;
using System.Linq;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models.Block;

namespace UniteBlocksRe.Models.BoardServices;

public static class UniteService
{
    public static Vector2I MinUniteSize { get; } = new Vector2I(2, 2);

    private readonly record struct Rect(Vector2I Origin, Vector2I Size);

    public static UniteResult Execute(BoardEntity board)
    {
        var steps = new List<UniteStep>();
        var visited = new HashSet<BlockEntity>();

        // 盤面を左上から順に走査
        for (var y = 0; y < BoardEntity.Size.Y; y++)
        {
            for (var x = 0; x < BoardEntity.Size.X; x++)
            {
                var currentPos = new Vector2I(x, y);

                // ブロックが存在しない、またはノーマル以外ならスキップ
                if (
                    board[currentPos] is not { Type: BlockType.Normal } startBlock
                    || visited.Contains(startBlock)
                )
                {
                    continue;
                }

                // このブロックを起点（左上）として、同じ色が形成する「最大かつ完璧な長方形」の範囲を特定
                var (rect, contains) = FindPerfectRectangle(board, startBlock);

                // 現在のサイズから変化しない場合はスキップ
                if (rect.Size == startBlock.Size)
                {
                    continue;
                }

                var targetBlocks = contains.ToList();
                // 範囲内のブロックを削除
                foreach (var b in targetBlocks)
                {
                    board.Remove(b);
                }
                // 合体後のブロックを設置
                var newBlock = BlockEntity.CreateNormal(startBlock.Color, rect.Size);
                board.Place(rect.Origin, newBlock);
                steps.Add(new UniteStep(targetBlocks, newBlock));
                visited.Add(newBlock);
            }
        }

        return new UniteResult(steps);
    }

    // 指定されたブロックを起点に、同色が連続している最大の長方形の範囲を計算する
    // 最小サイズを考慮する
    private static (Rect rect, HashSet<BlockEntity> contains) FindPerfectRectangle(
        BoardEntity board,
        BlockEntity startBlock
    )
    {
        HashSet<BlockEntity> containBlocks = [];
        var largestSize = startBlock.Size;
        var origin = board.GetPositionOf(startBlock);
        if (startBlock.Type != BlockType.Normal)
        {
            return (new(origin, startBlock.Size), containBlocks);
        }
        var color = startBlock.Color;

        // 考え得るサイズを全て考える
        for (var y = startBlock.Size.Y; origin.Y + y <= BoardEntity.Size.Y; y++)
        {
            for (var x = startBlock.Size.X; origin.X + x <= BoardEntity.Size.X; x++)
            {
                if (x * y <= largestSize.GetArea() || x < MinUniteSize.X || y < MinUniteSize.Y)
                {
                    continue;
                }

                // 範囲内が隙間なくはみ出しなく埋まっている事を確認
                var blocks = GetBlocksExclusivelyInArea(board, new(origin, new Vector2I(x, y)));
                if (blocks == null)
                {
                    continue;
                }

                // 範囲内のブロックが全て同色であることを確認
                if (blocks.All(b => b.Type == BlockType.Normal && b.Color == color))
                {
                    largestSize = new Vector2I(x, y);
                    containBlocks = blocks;
                }
            }
        }

        return (new(origin, largestSize), containBlocks);
    }

    // 指定範囲内に含まれるブロックを収集する。
    // 隙間がある、あるいははみ出しているブロックが存在する場合はnullを返す
    private static HashSet<BlockEntity>? GetBlocksExclusivelyInArea(BoardEntity board, Rect rect)
    {
        var found = new HashSet<BlockEntity>();
        for (var y = 0; y < rect.Size.Y; y++)
        {
            for (var x = 0; x < rect.Size.X; x++)
            {
                var pos = rect.Origin + new Vector2I(x, y);

                if (!BoardEntity.IsOutOfBounds(pos) && board[pos] is { } block)
                {
                    var origin = board.GetPositionOf(block);
                    if (!IsWithin(origin, block, rect))
                    {
                        return null; // はみ出していたらアウト
                    }
                    found.Add(block);
                }
                else
                {
                    return null; // 隙間があったらアウト
                }
            }
        }
        return found;
    }

    // rectの中に対象のブロックが収まっているか検証する
    private static bool IsWithin(Vector2I origin, BlockEntity b, Rect rect) =>
        origin.X >= rect.Origin.X
        && origin.Y >= rect.Origin.Y
        && origin.X + b.Size.X <= rect.Origin.X + rect.Size.X
        && origin.Y + b.Size.Y <= rect.Origin.Y + rect.Size.Y;
}

public sealed record UniteResult(IReadOnlyList<UniteStep> Steps) : IProcessStep
{
    public bool HasUnited => Steps.Count > 0;
}

public sealed record UniteStep(IReadOnlyList<BlockEntity> RemovedBlocks, BlockEntity CreatedBlock);

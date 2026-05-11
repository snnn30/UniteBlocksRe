using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace UniteBlocksRe.Models.BoardServices;

public static class ObstaclePlaceService
{
    public static int MaxPerColumn { get; } = 4;

    public static ObstaclePlaceResult Execute(BoardEntity board, int count)
    {
        var (capacities, minAltitudes) = CalculateColumnCapacities(board);
        var distribution = DistributeObstacles(count, capacities);
        var resultDict = ApplyPlacement(board, distribution, minAltitudes);
        return new ObstaclePlaceResult(resultDict);
    }

    // 各列の空きをチェック
    private static (int[] capacities, int[] minAltitudes) CalculateColumnCapacities(
        BoardEntity board
    )
    {
        var capacities = new int[BoardEntity.Size.X];
        var minAltitudes = new int[BoardEntity.Size.X];
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            var space = 0;

            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                if (board.CanPlace(new(x, y), Vector2I.One))
                {
                    space++;
                }
                else
                {
                    break;
                }
            }
            capacities[x] = Math.Min(space, MaxPerColumn);
            minAltitudes[x] = space - 1;
        }
        return (capacities, minAltitudes);
    }

    // 個数をどう配分するか
    private static int[] DistributeObstacles(int count, int[] capacities)
    {
        var currentCounts = new int[BoardEntity.Size.X];
        var remaining = count;

        // 均等分配
        while (true)
        {
            var canPlaceCol = Enumerable
                .Range(0, BoardEntity.Size.X)
                .Where(x => currentCounts[x] < capacities[x])
                .ToList();

            if (canPlaceCol.Count == 0 || remaining < canPlaceCol.Count)
            {
                break;
            }

            foreach (var col in canPlaceCol)
            {
                currentCounts[col]++;
                remaining--;
            }
        }

        // ランダム分配
        if (remaining > 0)
        {
            var rand = new Random();
            var canPlaceCol = Enumerable
                .Range(0, BoardEntity.Size.X)
                .Where(x => currentCounts[x] < capacities[x])
                .ToList();

            while (remaining > 0 && canPlaceCol.Count > 0)
            {
                var col = canPlaceCol[rand.Next(canPlaceCol.Count)];
                currentCounts[col]++;
                remaining--;
                canPlaceCol.Remove(col);
            }
        }
        return currentCounts;
    }

    // 最終的な配置処理
    private static Dictionary<int, ColumnResult> ApplyPlacement(
        BoardEntity board,
        int[] distribution,
        int[] minAltitudes
    )
    {
        var result = new Dictionary<int, ColumnResult>();
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            if (distribution[x] <= 0)
            {
                continue;
            }

            List<(BlockEntity block, Vector2I position)> blocks = [];
            for (var i = 0; i < distribution[x]; i++)
            {
                var y = minAltitudes[x] - i;
                var block = BlockEntity.CreateObstacle();
                var pos = new Vector2I(x, y);
                board.Place(pos, block);
                blocks.Add((block, pos));
            }
            result.Add(x, new ColumnResult(blocks));
        }
        return result;
    }
}

public sealed record ObstaclePlaceResult(IReadOnlyDictionary<int, ColumnResult> Colmuns)
{
    public bool Placed => Colmuns.Count > 0;
    public int PlacedCount => Colmuns.Values.Sum(c => c.Blocks.Count);
}

public sealed record ColumnResult(IReadOnlyList<(BlockEntity block, Vector2I position)> Blocks);

using System;
using System.Collections.Generic;
using Godot;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

namespace UniteBlocksRe.src.Models.Services;

public static class BoardObstaclePlacer
{
    public static ObstaclePlaceResult Place(BoardEntity board, int count, int maxPerColumn)
    {
        var colCapacities = new int[BoardEntity.Size.X];
        var currentCounts = new int[BoardEntity.Size.X];
        var minAltitude = new int[BoardEntity.Size.X];
        var remaining = count;

        // 各列の空き容量を確認
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            var space = 0;

            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                if (board.CanPlace(new(x, y), BlockEntity.Obstacle))
                {
                    space++;
                }
                else
                {
                    break;
                }
            }
            colCapacities[x] = Mathf.Min(space, maxPerColumn);
            currentCounts[x] = 0;
            minAltitude[x] = space - 1;
        }

        // １段ずつ埋める　端数が出るまでやる
        var noPlace = false;
        while (true)
        {
            List<int> canPlaceCol = [];
            for (var x = 0; x < BoardEntity.Size.X; x++)
            {
                if (currentCounts[x] < colCapacities[x])
                {
                    canPlaceCol.Add(x);
                }
            }

            if (canPlaceCol.Count == 0)
            {
                noPlace = true;
                break;
            }

            if (remaining < canPlaceCol.Count)
            {
                break;
            }

            foreach (var col in canPlaceCol)
            {
                currentCounts[col]++;
                remaining--;
            }
        }

        // 端数をランダム分配
        if (remaining > 0 && !noPlace)
        {
            List<int> canPlaceCol = [];
            for (var x = 0; x < BoardEntity.Size.X; x++)
            {
                if (currentCounts[x] < colCapacities[x])
                {
                    canPlaceCol.Add(x);
                }
            }

            var rand = new Random();
            while (remaining > 0 && canPlaceCol.Count > 0)
            {
                var col = canPlaceCol[rand.Next(canPlaceCol.Count)];

                currentCounts[col]++;
                remaining--;

                canPlaceCol.Remove(col);
            }
        }

        // 座標の算出、実際に配置

        Dictionary<int, ColumnResult> result = [];

        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            List<(BlockEntity block, Vector2I position)> blocks = [];
            for (var i = 0; i < currentCounts[x]; i++)
            {
                var y = minAltitude[x] - i;
                var block = BlockEntity.Obstacle;
                var pos = new Vector2I(x, y);
                board.TrySetBlock(pos, block);
                blocks.Add((block, pos));
            }

            if (blocks.Count > 0)
            {
                result.Add(x, new(blocks));
            }
        }

        return new ObstaclePlaceResult(result);
    }
}

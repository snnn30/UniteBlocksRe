using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Common;

namespace UniteBlocksRe.Models;

public class BoardEntity : Entity<BoardEntity>, IEnumerable<(BlockEntity Block, Vector2I Pos)>
{
    // 左上を原点とする座標系で、右方向がX軸、下方向がY軸
    public static readonly Vector2I Size = new(8, 14);
    public static readonly Vector2I SpawnPosition = new(3, 1);

    private readonly BlockEntity[,] _grid = new BlockEntity[Size.X, Size.Y];
    private readonly Dictionary<BlockEntity, Vector2I> _blockToPos = [];

    public BlockEntity this[int x, int y]
    {
        get => _grid[x, y];
        private set => _grid[x, y] = value;
    }

    public BlockEntity this[Vector2I v]
    {
        get => _grid[v.X, v.Y];
        private set => _grid[v.X, v.Y] = value;
    }

    public IEnumerator<(BlockEntity Block, Vector2I Pos)> GetEnumerator()
    {
        foreach (var pair in _blockToPos)
        {
            yield return (pair.Key, pair.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #region Command

    public void Place(Vector2I origin, BlockEntity block)
    {
        if (!CanPlace(origin, block))
        {
            throw new InvalidOperationException("配置不可能な座標指定");
        }
        _blockToPos.Add(block, origin);
        foreach (var p in GetOccupiedPositions(origin, block.Size))
        {
            this[p] = block;
        }
    }

    public void Remove(BlockEntity block)
    {
        foreach (var p in GetOccupiedPositions(block))
        {
            this[p] = null;
        }
        _blockToPos.Remove(block);
    }

    #endregion
    #region Query

    public bool CanPlace(Vector2I origin, BlockEntity block) => CanPlace(origin, block.Size);

    public bool CanPlace(Vector2I origin, Vector2I size)
    {
        foreach (var p in GetOccupiedPositions(origin, size))
        {
            if (IsOutOfBounds(p) || this[p] != null)
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsOutOfBounds(Vector2I pos) =>
        pos.X < 0 || pos.Y < 0 || pos.X >= Size.X || pos.Y >= Size.Y;

    public IEnumerable<BlockEntity> GetAdjacentBlocks(BlockEntity block)
    {
        var origin = GetPositionOf(block);
        var adjacentBlocks = new HashSet<BlockEntity>();

        foreach (var p in GetOccupiedPositions(block))
        {
            var neighbors = new[]
            {
                p + Vector2I.Up,
                p + Vector2I.Down,
                p + Vector2I.Left,
                p + Vector2I.Right,
            };

            foreach (var n in neighbors)
            {
                if (!IsOutOfBounds(n) && this[n] is BlockEntity other && other != block)
                {
                    adjacentBlocks.Add(other);
                }
            }
        }

        return adjacentBlocks;
    }

    public IEnumerable<Vector2I> GetOccupiedPositions(BlockEntity block)
    {
        var origin = GetPositionOf(block);
        for (var x = 0; x < block.Size.X; x++)
        {
            for (var y = 0; y < block.Size.Y; y++)
            {
                yield return origin + new Vector2I(x, y);
            }
        }
    }

    public static IEnumerable<Vector2I> GetOccupiedPositions(Vector2I origin, Vector2I size)
    {
        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                yield return origin + new Vector2I(x, y);
            }
        }
    }

    public Vector2I GetPositionOf(BlockEntity block)
    {
        if (_blockToPos.TryGetValue(block, out var pos))
        {
            return pos;
        }
        throw new InvalidOperationException($"指定されたブロック {block} は盤面上に存在しません。");
    }

    #endregion

    public BoardEntity Clone()
    {
        var clone = new BoardEntity();

        // Placeを呼び出すことで _grid と _blockToPos の両方を正しく再構築する
        foreach (var (block, pos) in this)
        {
            clone.Place(pos, block);
        }

        return clone;
    }
}

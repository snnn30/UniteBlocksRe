using System.Collections.Generic;
using Godot;

namespace UniteBlocksRe.Models.Entities;

public class BoardEntity
{
    // 左上を原点とする座標系で、右方向がX軸、下方向がY軸
    public static readonly Vector2I Size = new(8, 12);
    public static readonly Vector2I SpawnPosition = new(3, 1);

    private readonly BlockEntity[,] _grid = new BlockEntity[Size.X, Size.Y];
    private readonly Dictionary<BlockEntity, Vector2I> _blockOrigins = [];

    public bool IsWithinBounds(Vector2I position)
    {
        return position.X >= 0 && position.X < Size.X && position.Y >= 0 && position.Y < Size.Y;
    }

    public (bool Sucess, BlockEntity block) TryGetBlock(Vector2I pos)
    {
        if (!IsWithinBounds(pos))
        {
            return (false, null);
        }
        var block = _grid[pos.X, pos.Y];
        return (block != null, block);
    }

    public (bool Sucess, Vector2I Position) TryGetOrigin(BlockEntity entity)
    {
        if (!_blockOrigins.TryGetValue(entity, out var origin))
        {
            return (false, default);
        }
        return (true, origin);
    }

    public bool TrySetBlock(Vector2I origin, BlockEntity entity)
    {
        if (!CanPlace(origin, entity))
        {
            return false;
        }

        _blockOrigins[entity] = origin;
        foreach (var contain in CalculateContains(origin, entity.Size))
        {
            _grid[contain.X, contain.Y] = entity;
        }

        return true;
    }

    public bool TryRemoveBlockAt(Vector2I pos)
    {
        var result = TryGetBlock(pos);
        if (!result.Sucess)
        {
            return false;
        }

        return TryRemoveBlock(result.block);
    }

    public bool TryRemoveBlock(BlockEntity target)
    {
        if (!_blockOrigins.TryGetValue(target, out var origin))
        {
            return false;
        }

        foreach (var cell in CalculateContains(origin, target.Size))
        {
            _grid[cell.X, cell.Y] = null;
        }

        _blockOrigins.Remove(target);

        return true;
    }

    public bool CanPlace(Vector2I origin, BlockEntity entity, BlockEntity ignoreBlock = null)
    {
        return CanPlace(origin, entity.Size, ignoreBlock);
    }

    public bool CanPlace(Vector2I origin, Vector2I size, BlockEntity ignoreBlock = null)
    {
        foreach (var cell in CalculateContains(origin, size))
        {
            if (!IsWithinBounds(cell))
            {
                return false;
            }

            var result = TryGetBlock(cell);
            if (result.Sucess && result.block != ignoreBlock)
            {
                return false;
            }
        }

        return true;
    }

    public static IEnumerable<Vector2I> CalculateContains(Vector2I origin, Vector2I size)
    {
        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                yield return new Vector2I(origin.X + x, origin.Y + y);
            }
        }
    }
}

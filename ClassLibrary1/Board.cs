using System.Collections.Immutable;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain;

public sealed record Board : Entity
{
    public ImmutableDictionary<Vector2I, Block> Grid { get; }
    public Vector2I Bounds { get; }
    public Vector2I SpawnPos { get; }

    public Board(Vector2I bounds, Vector2I spawnPos)
    {
        Bounds = bounds;
        if (IsOutOfBounds(spawnPos))
        {
            throw new ArgumentOutOfRangeException(
                nameof(spawnPos),
                $"スポーン地点 {spawnPos} はボードの範囲外 {bounds}."
            );
        }
        SpawnPos = spawnPos;
        Grid = ImmutableDictionary<Vector2I, Block>.Empty;
    }

    private Board(Board board, ImmutableDictionary<Vector2I, Block> grid)
    {
        Bounds = board.Bounds;
        SpawnPos = board.SpawnPos;
        Grid = grid;
    }

    #region Command

    public Board Place(Vector2I origin, Block block)
    {
        if (!CanPlace(origin, block))
        {
            throw new InvalidOperationException("配置不可能な座標指定");
        }
        var gridBuilder = Grid.ToBuilder();
        foreach (var p in GetOccupiedPositions(origin, block.Size))
        {
            gridBuilder.Add(p, block);
        }
        return new Board(this, gridBuilder.ToImmutable());
    }

    public Board Remove(Block block)
    {
        var gridBuilder = Grid.ToBuilder();
        foreach (var p in GetOccupiedPositions(block))
        {
            gridBuilder.Remove(p);
        }
        return new Board(this, gridBuilder.ToImmutable());
    }

    #endregion

    #region Query

    public bool CanPlace(Vector2I origin, Block block) => CanPlace(origin, block.Size);

    public bool CanPlace(Vector2I origin, Vector2I size)
    {
        foreach (var p in GetOccupiedPositions(origin, size))
        {
            if (IsOutOfBounds(p) || Grid.ContainsKey(p))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsOutOfBounds(Vector2I pos) =>
        pos.X < 0 || pos.Y < 0 || pos.X >= Bounds.X || pos.Y >= Bounds.Y;

    public IEnumerable<Block> GetAdjacentBlocks(Block block)
    {
        var origin = GetPositionOf(block);
        var adjacentBlocks = new HashSet<Block>();

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
                if (
                    !IsOutOfBounds(n)
                    && Grid.TryGetValue(n, out var otherBlock)
                    && otherBlock != block
                )
                {
                    adjacentBlocks.Add(otherBlock);
                }
            }
        }

        return adjacentBlocks;
    }

    public IEnumerable<Vector2I> GetOccupiedPositions(Block block)
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

    public Vector2I GetPositionOf(Block block)
    {
        var positions = Grid.Where(x => x.Value == block).Select(x => x.Key).ToList();
        if (positions.Count == 0)
        {
            throw new InvalidOperationException($"指定されたブロック {block} は盤面上に存在しない");
        }
        var minX = positions.Min(p => p.X);
        var minY = positions.Min(p => p.Y);
        return new Vector2I(minX, minY);
    }

    #endregion
}

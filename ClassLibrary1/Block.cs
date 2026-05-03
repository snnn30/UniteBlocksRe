using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain;

public abstract record Block : Entity
{
    private Vector2I _size;
    public Vector2I Size
    {
        get => _size;
        set
        {
            ValidateSize(value);
            _size = value;
        }
    }

    public Block(Vector2I size)
    {
        Size = size;
    }

    private void ValidateSize(Vector2I size)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentException("サイズは (1,1) 以上である必要があります", nameof(Size));
        }
    }
}

public sealed record NormalBlock : Block
{
    public BlockColor Color { get; set; }

    public NormalBlock(Vector2I size, BlockColor color)
        : base(size) => Color = color;

    public NormalBlock(BlockColor color)
        : this(Vector2I.One, color) { }
}

public sealed record BombBlock() : Block(Vector2I.One);

public sealed record ObstacleBlock() : Block(Vector2I.One);

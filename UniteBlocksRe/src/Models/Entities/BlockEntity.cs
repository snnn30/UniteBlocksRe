using Godot;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Entities;

public class BlockEntity
{
    public BlockType Type { get; init; }
    public BlockColor Color { get; init; }
    public Vector2I Size { get; init; }

    public static BlockEntity Bomb => new(BlockType.Bomb, BlockColor.None, Vector2I.One);

    public static BlockEntity Obstacle => new(BlockType.Obstacle, BlockColor.None, Vector2I.One);

    public BlockEntity(BlockColor color)
    {
        Type = BlockType.Normal;
        Color = color;
        Size = Vector2I.One;
    }

    public BlockEntity(BlockType type, BlockColor color, Vector2I size)
    {
        Type = type;
        Color = color;
        Size = size;
    }

    public override string ToString()
    {
        return $"BlockEntity(Type:{Type}, Color:{Color}, Size:{Size.X}x{Size.Y})";
    }
}

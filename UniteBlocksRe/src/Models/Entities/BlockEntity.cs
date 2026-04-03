using Godot;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Entities;

public class BlockEntity
{
    public BlockColor Color { get; init; }
    public Vector2I Size { get; init; }

    public BlockEntity(BlockColor color, Vector2I size)
    {
        Color = color;
        Size = size;
    }

    public override string ToString()
    {
        return $"BlockEntity(Color:{Color}, Size:{Size.X}x{Size.Y})";
    }
}

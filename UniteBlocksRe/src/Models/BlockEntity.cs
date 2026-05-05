using Godot;
using UniteBlocksRe.Common;
using UniteBlocksRe.Models.Block;

namespace UniteBlocksRe.Models;

/// <summary>
/// ブロックを表現するエンティティ
/// </summary>
/// <remarks>
/// このクラスは不変(Immutable)です。
/// </remarks>
public class BlockEntity : Entity<BlockEntity>
{
    public BlockType Type { get; }
    public BlockColor Color { get; }
    public Vector2I Size { get; }

    public static BlockEntity CreateBomb() => new(BlockType.Bomb, BlockColor.None, Vector2I.One);

    public static BlockEntity CreateObstacle() =>
        new(BlockType.Obstacle, BlockColor.None, Vector2I.One);

    public static BlockEntity CreateNormal(BlockColor color) =>
        new(BlockType.Normal, color, Vector2I.One);

    public static BlockEntity CreateNormal(BlockColor color, Vector2I size) =>
        new(BlockType.Normal, color, size);

    private BlockEntity(BlockType type, BlockColor color, Vector2I size)
    {
        Type = type;
        Color = color;
        Size = size;
    }

    public override string ToString()
    {
        return $"BlockEntity(ID:{Id} Type:{Type}, Color:{Color}, Size:{Size.X}x{Size.Y})";
    }
}

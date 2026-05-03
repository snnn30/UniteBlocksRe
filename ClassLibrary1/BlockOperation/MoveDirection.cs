using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.BlockOperation;

public sealed class MoveDirection : Enumeration<MoveDirection>
{
    public static readonly MoveDirection Left = new(0, nameof(Left), Vector2I.Left);
    public static readonly MoveDirection Right = new(1, nameof(Right), Vector2I.Right);

    public Vector2I Offset { get; }

    private MoveDirection(int id, string name, Vector2I offset)
        : base(id, name)
    {
        Offset = offset;
    }
}

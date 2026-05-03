using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.BlockOperation;

public sealed class RotateDirection : Enumeration<RotateDirection>
{
    public static readonly RotateDirection ACW = new(0, nameof(ACW));
    public static readonly RotateDirection CW = new(1, nameof(CW));

    private RotateDirection(int id, string name)
        : base(id, name) { }
}

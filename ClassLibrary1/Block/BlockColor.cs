using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain.Blocks;

public sealed class BlockColor : Enumeration<BlockColor>
{
    public static readonly BlockColor Red = new(0, nameof(Red));
    public static readonly BlockColor Blue = new(1, nameof(Blue));
    public static readonly BlockColor Green = new(2, nameof(Green));
    public static readonly BlockColor Orange = new(3, nameof(Orange));

    private BlockColor(int id, string name)
        : base(id, name) { }
}

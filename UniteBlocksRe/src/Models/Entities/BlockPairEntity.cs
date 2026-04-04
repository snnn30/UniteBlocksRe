namespace UniteBlocksRe.Models.Entities;

public class BlockPairEntity
{
    BlockEntity Parent { get; init; }
    BlockEntity Child { get; init; }

    public BlockPairEntity(BlockEntity parent, BlockEntity child)
    {
        Parent = parent;
        Child = child;
    }
}

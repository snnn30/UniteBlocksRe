using System;
using UniteBlocksRe.src.Common;
using UniteBlocksRe.src.Models.Block;
using BlockPair = (
    UniteBlocksRe.src.Models.BlockEntity Parent,
    UniteBlocksRe.src.Models.BlockEntity Child
);

namespace UniteBlocksRe.src.Models;

public class BlockQueueEntity : Entity<BlockQueueEntity>
{
    public BlockPair Next { get; private set; }
    public BlockPair NextNext { get; private set; }

    public BlockQueueEntity()
    {
        Next = GeneratePair();
        NextNext = GeneratePair();
    }

    public BlockPair Dequeue()
    {
        var current = Next;
        Next = NextNext;
        NextNext = GeneratePair();
        return current;
    }

    private static BlockPair GeneratePair()
    {
        var parent = BlockEntity.CreateNormal(GetRandomColor());
        var child = BlockEntity.CreateNormal(GetRandomColor());
        return new(parent, child);
    }

    private static BlockColor GetRandomColor()
    {
        var values = Enum.GetValues<BlockColor>();
        var random = new Random();
        return values[random.Next(1, values.Length)];
    }
}

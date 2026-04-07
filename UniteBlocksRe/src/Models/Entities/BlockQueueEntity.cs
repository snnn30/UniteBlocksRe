using System;
using UniteBlocksRe.Models.ValueObjects;
using BlockPair = (
    UniteBlocksRe.Models.Entities.BlockEntity Parent,
    UniteBlocksRe.Models.Entities.BlockEntity Child
);

namespace UniteBlocksRe.Models.Entities;

public class BlockQueueEntity
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
        BlockEntity parent = new(BlockType.Normal, GetRandomColor(), new(1, 1));
        BlockEntity child = new(BlockType.Normal, GetRandomColor(), new(1, 1));
        return new(parent, child);
    }

    private static BlockColor GetRandomColor()
    {
        var values = Enum.GetValues<BlockColor>();
        var random = new Random();
        return values[random.Next(1, values.Length)];
    }
}

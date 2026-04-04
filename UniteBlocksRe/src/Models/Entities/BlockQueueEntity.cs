using System;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Models.Entities;

public class BlockQueueEntity
{
    public BlockPairEntity Next { get; private set; }
    public BlockPairEntity NextNext { get; private set; }

    public BlockQueueEntity()
    {
        Next = GeneratePair();
        NextNext = GeneratePair();
    }

    public BlockPairEntity Dequeue()
    {
        var current = Next;
        Next = NextNext;
        NextNext = GeneratePair();
        return current;
    }

    private static BlockPairEntity GeneratePair()
    {
        BlockEntity parent = new(GetRandomColor(), new(1, 1));
        BlockEntity child = new(GetRandomColor(), new(1, 1));
        return new(parent, child);
    }

    private static BlockColor GetRandomColor()
    {
        var values = Enum.GetValues<BlockColor>();
        var random = new Random();
        return values[random.Next(values.Length)];
    }
}

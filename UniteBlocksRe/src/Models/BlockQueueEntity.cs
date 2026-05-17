using System;
using UniteBlocksRe.Models.Block;
using BlockPair = (UniteBlocksRe.Models.BlockEntity Parent, UniteBlocksRe.Models.BlockEntity Child);

namespace UniteBlocksRe.Models;

public sealed class BlockQueueEntity
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

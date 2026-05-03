using System.Collections.Immutable;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Common;
using BlockPair = (UniteBlocksRe.Domain.Block Parent, UniteBlocksRe.Domain.Block Child);

namespace UniteBlocksRe.Domain;

public sealed record BlockPairQueue : Entity
{
    public ImmutableQueue<BlockPair> Pairs { get; }

    public BlockPairQueue(int size)
    {
        if (size < 1)
        {
            throw new ArgumentException("キューのサイズは1以上である必要がある");
        }

        // 指定された数だけ最初に生成して埋める
        ImmutableQueue<BlockPair> pairs = [];
        for (var i = 0; i < size; i++)
        {
            pairs = pairs.Enqueue(GeneratePair());
        }
        Pairs = pairs;
    }

    private BlockPairQueue(ImmutableQueue<BlockPair> pairs)
    {
        Pairs = pairs;
    }

    public BlockPairQueue Dequeue(out BlockPair pair)
    {
        var nextQueue = Pairs.Dequeue(out pair);
        nextQueue = nextQueue.Enqueue(GeneratePair());
        return new(nextQueue);
    }

    private static BlockPair GeneratePair()
    {
        Block parent = new NormalBlock(GetRandomColor());
        Block child = new NormalBlock(GetRandomColor());
        return new(parent, child);
    }

    private static BlockColor GetRandomColor()
    {
        var colors = BlockColor.All.ToArray();
        var random = new Random();
        return colors[random.Next(colors.Length)];
    }
}

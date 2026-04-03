using Shouldly;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.Services;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Services;

public class BoardExploderTests
{
    [Fact(DisplayName = "1つだけ爆発するテスト")]
    public void Test1()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block);
        var result = BoardExploder.Explode(board, block);

        result.ChainCount.ShouldBe(1);
        result.Steps[0].Exploded.Count.ShouldBe(1);
        result.Steps[0].Exploded.First().ShouldBe(block);
    }

    [Fact(DisplayName = "隣接するブロックも爆発するテスト")]
    public void Test2()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(1, 0), block2);
        board.TrySetBlock(new(1, 1), block3);
        var result = BoardExploder.Explode(board, block1);

        result.ChainCount.ShouldBe(3);
        result.Steps[0].Exploded.First().ShouldBe(block1);
        result.Steps[1].Exploded.First().ShouldBe(block2);
        result.Steps[2].Exploded.First().ShouldBe(block3);
    }

    [Fact(DisplayName = "隣接していないブロックは爆発しないテスト")]
    public void Test3()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(2, 0), block2);
        var result = BoardExploder.Explode(board, block1);

        result.ChainCount.ShouldBe(1);
        result.Steps[0].Exploded.First().ShouldBe(block1);
    }

    [Fact(DisplayName = "異なる色のブロックは爆発しないテスト")]
    public void Test4()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockColor.Blue, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(1, 0), block2);
        var result = BoardExploder.Explode(board, block1);

        result.ChainCount.ShouldBe(1);
        result.Steps[0].Exploded.First().ShouldBe(block1);
    }

    [Fact(DisplayName = "複数のブロックが同時に爆発するテスト")]
    public void Test5()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(1, 0), block2);
        board.TrySetBlock(new(2, 0), block3);
        var result = BoardExploder.Explode(board, block2);

        result.ChainCount.ShouldBe(2);
        result.Steps[0].Exploded.Count.ShouldBe(1);
        result.Steps[0].Exploded.ShouldContain(block2);
        result.Steps[1].Exploded.Count.ShouldBe(2);
        result.Steps[1].Exploded.ShouldContain(block1);
        result.Steps[1].Exploded.ShouldContain(block3);
    }

    [Fact(DisplayName = "塊が爆発するテスト")]
    public void Test6()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(3, 3));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(1, 1), block1);
        board.TrySetBlock(new(3, 0), block2);
        board.TrySetBlock(new(0, 2), block3);
        var result = BoardExploder.Explode(board, block1);

        result.ChainCount.ShouldBe(2);
        result.Steps[0].Exploded.Count.ShouldBe(1);
        result.Steps[0].Exploded.First().ShouldBe(block1);
        result.Steps[1].Exploded.Count.ShouldBe(2);
        result.Steps[1].Exploded.ShouldContain(block2);
        result.Steps[1].Exploded.ShouldContain(block3);
    }
}

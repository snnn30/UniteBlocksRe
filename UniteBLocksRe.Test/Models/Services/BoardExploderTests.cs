using Shouldly;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.Services;
using UniteBlocksRe.src.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Services;

public class BoardExploderTests
{
    [Fact(DisplayName = "ブロックが1つだけ爆発するテスト")]
    public void Test1()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block = new BlockEntity(BlockColor.Red);

        board.TrySetBlock(new(0, 0), bomb);
        board.TrySetBlock(new(0, 1), block);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(2);
        result.Steps[0].Exploded.Count.ShouldBe(1);
        result.Steps[0].Exploded.First().ShouldBe(bomb);
        result.Steps[1].Exploded.Count().ShouldBe(1);
        result.Steps[1].Exploded.First().ShouldBe(block);
    }

    [Fact(DisplayName = "隣接するブロックも爆発するテスト")]
    public void Test2()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), bomb);
        board.TrySetBlock(new(0, 1), block1);
        board.TrySetBlock(new(1, 1), block2);
        board.TrySetBlock(new(1, 2), block3);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(4);
        result.Steps[1].Exploded.First().ShouldBe(block1);
        result.Steps[2].Exploded.First().ShouldBe(block2);
        result.Steps[3].Exploded.First().ShouldBe(block3);
    }

    [Fact(DisplayName = "隣接していないブロックは爆発しないテスト")]
    public void Test3()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), bomb);
        board.TrySetBlock(new(0, 1), block1);
        board.TrySetBlock(new(2, 1), block2);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(2);
        result.Steps[1].Exploded.First().ShouldBe(block1);
    }

    [Fact(DisplayName = "異なる色のブロックは爆発しないテスト")]
    public void Test4()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Blue, new(1, 1));

        board.TrySetBlock(new(0, 0), bomb);
        board.TrySetBlock(new(0, 1), block1);
        board.TrySetBlock(new(1, 1), block2);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(2);
        result.Steps[1].Exploded.First().ShouldBe(block1);
    }

    [Fact(DisplayName = "複数のブロックが同時に爆発するテスト")]
    public void Test5()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(1, 0), bomb);
        board.TrySetBlock(new(0, 1), block1);
        board.TrySetBlock(new(1, 1), block2);
        board.TrySetBlock(new(2, 1), block3);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(3);
        result.Steps[1].Exploded.Count.ShouldBe(1);
        result.Steps[1].Exploded.ShouldContain(block2);
        result.Steps[2].Exploded.Count.ShouldBe(2);
        result.Steps[2].Exploded.ShouldContain(block1);
        result.Steps[2].Exploded.ShouldContain(block3);
    }

    [Fact(DisplayName = "塊が爆発するテスト")]
    public void Test6()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.Bomb;
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(3, 3));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(1, 0), bomb);
        board.TrySetBlock(new(1, 1), block1);
        board.TrySetBlock(new(3, 0), block2);
        board.TrySetBlock(new(0, 2), block3);
        var result = BoardExploder.Explode(board, bomb);

        result.ChainCount.ShouldBe(3);
        result.Steps[1].Exploded.Count.ShouldBe(1);
        result.Steps[1].Exploded.First().ShouldBe(block1);
        result.Steps[2].Exploded.Count.ShouldBe(2);
        result.Steps[2].Exploded.ShouldContain(block2);
        result.Steps[2].Exploded.ShouldContain(block3);
    }
}

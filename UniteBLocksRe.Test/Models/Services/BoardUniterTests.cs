using Godot;
using Shouldly;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.Services;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Services;

public class BoardUniterTests
{
    [Fact(DisplayName = "基本的な合体テスト1")]
    public void Test1()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(2, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(2, 1));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(0, 1), block2);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeTrue();
        result.ChainCount.ShouldBe(1);
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
    }

    [Fact(DisplayName = "基本的な合体テスト2")]
    public void Test2()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block3 = new BlockEntity(BlockColor.Red, new(1, 1));
        var block4 = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(0, 1), block2);
        board.TrySetBlock(new(1, 0), block3);
        board.TrySetBlock(new(1, 1), block4);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeTrue();
        result.ChainCount.ShouldBe(1);
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
    }

    [Fact(DisplayName = "合体できないパターンのテスト 色違い")]
    public void Test3()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(2, 1));
        var block2 = new BlockEntity(BlockColor.Green, new(2, 1));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(0, 1), block2);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeFalse();
        result.ChainCount.ShouldBe(0);
    }

    [Fact(DisplayName = "合体できないパターンのテスト はみ出し")]
    public void Test4()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(1, 2));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 3));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(1, 0), block2);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeFalse();
        result.ChainCount.ShouldBe(0);
    }

    [Fact(DisplayName = "可能な限り大きくするテスト")]
    public void Test5()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(2, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(1, 2));
        var block3 = new BlockEntity(BlockColor.Red, new(2, 1));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(2, 0), block2);
        board.TrySetBlock(new(0, 1), block3);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeTrue();
        result.ChainCount.ShouldBe(1);
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(3, 2));
        result.Steps[0].Position.ShouldBe(new Vector2I(0, 0));
    }

    [Fact(DisplayName = "複数回の合体のテスト")]
    public void Test6()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(2, 1));
        var block2 = new BlockEntity(BlockColor.Red, new(2, 1));
        var block3 = new BlockEntity(BlockColor.Blue, new(2, 1));
        var block4 = new BlockEntity(BlockColor.Blue, new(2, 2));

        board.TrySetBlock(new(0, 0), block);
        board.TrySetBlock(new(0, 1), block2);
        board.TrySetBlock(new(5, BoardEntity.Size.Y - 1), block3);
        board.TrySetBlock(new(5, BoardEntity.Size.Y - 3), block4);
        var result = BoardUniter.Unite(board);

        result.HasUnited.ShouldBeTrue();
        result.ChainCount.ShouldBe(2);
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
        result.Steps[0].Position.ShouldBe(new Vector2I(0, 0));
        result.Steps[1].CreatedBlock.Size.ShouldBe(new Vector2I(2, 3));
        result.Steps[1].Position.ShouldBe(new Vector2I(5, BoardEntity.Size.Y - 3));
    }
}

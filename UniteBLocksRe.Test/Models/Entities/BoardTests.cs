using Godot;
using Shouldly;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Entities;

public class BoardTests
{
    [Fact(DisplayName = "基本操作のテスト")]
    public void Test1()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block).ShouldBeTrue();
        board.TryGetBlock(new(0, 0)).ShouldBe((true, block));
        board.TryGetOrigin(block).ShouldBe((true, new(0, 0)));
        board.TryRemoveBlock(block).ShouldBeTrue();
        board.TryGetBlock(new(0, 0)).ShouldBe((false, null));
    }

    [Fact(DisplayName = "画面端のテスト")]
    public void Test2()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockColor.Red, new(1, 1));

        board.CanPlace(BoardEntity.Size - new Vector2I(1, 1), block).ShouldBeTrue();
        board.CanPlace(new(-1, 0), block).ShouldBeFalse();
        board.CanPlace(new(0, -1), block).ShouldBeFalse();
        board.CanPlace(new(BoardEntity.Size.X, 0), block).ShouldBeFalse();
        board.CanPlace(new(0, BoardEntity.Size.Y), block).ShouldBeFalse();

        board.CanPlace(BoardEntity.Size - new Vector2I(1, 1), block).ShouldBeTrue();
        var block2 = new BlockEntity(BlockColor.Green, new(2, 1));
        board.CanPlace(BoardEntity.Size - new Vector2I(1, 0), block2).ShouldBeFalse();
    }

    [Fact(DisplayName = "重なりのテスト")]
    public void Test3()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockColor.Red, new(2, 2));
        var block2 = new BlockEntity(BlockColor.Green, new(2, 2));

        board.TrySetBlock(new(0, 0), block1).ShouldBeTrue();
        board.CanPlace(new(1, 1), block2).ShouldBeFalse();
        board.CanPlace(new(2, 2), block2).ShouldBeTrue();
    }
}

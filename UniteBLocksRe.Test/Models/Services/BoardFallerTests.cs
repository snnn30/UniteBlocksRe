using Godot;
using Shouldly;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.Services;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Services;

public class BoardFallerTests
{
    [Fact(DisplayName = "基本的な落下テスト")]
    public void Test1()
    {
        var board = new BoardEntity();
        var block = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block);
        var result = BoardFaller.Fall(board);

        result.Steps.Count.ShouldBe(1);
        result.Steps[0].From.ShouldBe(new Vector2I(0, 0));
        result.Steps[0].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));
    }

    [Fact(DisplayName = "縦に隣接したブロックの落下テスト")]
    public void Test2()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(0, 1), block2);
        var result = BoardFaller.Fall(board);

        result.Steps.Count.ShouldBe(2);
        result.Steps[0].From.ShouldBe(new Vector2I(0, 1));
        result.Steps[0].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));
        result.Steps[1].From.ShouldBe(new Vector2I(0, 0));
        result.Steps[1].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 2));
    }

    [Fact(DisplayName = "形状の異なる隣接ブロックの落下テスト")]
    public void Test3()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(2, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, 0), block1);
        board.TrySetBlock(new(1, 1), block2);
        var result = BoardFaller.Fall(board);

        result.Steps.Count.ShouldBe(2);
        result.Steps[0].From.ShouldBe(new Vector2I(1, 1));
        result.Steps[0].To.ShouldBe(new Vector2I(1, BoardEntity.Size.Y - 1));
        result.Steps[1].From.ShouldBe(new Vector2I(0, 0));
        result.Steps[1].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 2));
    }

    [Fact(DisplayName = "落下できないブロックのテスト")]
    public void Test4()
    {
        var board = new BoardEntity();
        var block1 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));
        var block2 = new BlockEntity(BlockType.Normal, BlockColor.Red, new(1, 1));

        board.TrySetBlock(new(0, BoardEntity.Size.Y - 1), block1);
        board.TrySetBlock(new(0, BoardEntity.Size.Y - 2), block2);
        var result = BoardFaller.Fall(board);

        result.HasChanged.ShouldBeFalse();
    }
}

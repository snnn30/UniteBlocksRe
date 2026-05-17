using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Test.Models.BoardServices;

public class FallServiceTests
{
    [Fact(DisplayName = "1つのブロックが最下段まで落ちるテスト")]
    public void SingleBlock_ShouldFall_ToBottomMostRow()
    {
        var board = new BoardEntity();
        var block = BlockEntity.CreateNormal(BlockColor.Red);
        board.Place(Vector2I.Zero, block);

        var result = FallService.Execute(board);

        result.Movements.Count.ShouldBe(1);
        result.Movements[0].From.ShouldBe(Vector2I.Zero);
        result.Movements[0].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));

        var finalPos = board.GetPositionOf(block);
        finalPos.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));
    }

    [Fact(DisplayName = "縦に隣接したブロックが下のブロックから順に詰めて落ちるテスト")]
    public void VerticallyStackedBlocks_ShouldFall_SequentiallyToBottom()
    {
        var board = new BoardEntity();
        var upperBlock = BlockEntity.CreateNormal(BlockColor.Red);
        var lowerBlock = BlockEntity.CreateNormal(BlockColor.Blue);

        board.Place(new Vector2I(0, 0), upperBlock);
        board.Place(new Vector2I(0, 1), lowerBlock);

        var result = FallService.Execute(board);

        result.Movements.Count.ShouldBe(2);

        var maxY = BoardEntity.Size.Y - 1;
        board.GetPositionOf(lowerBlock).ShouldBe(new Vector2I(0, maxY));
        board.GetPositionOf(upperBlock).ShouldBe(new Vector2I(0, maxY - 1));
    }

    [Fact(DisplayName = "雑な形状でも正しく重なるかのテスト")]
    public void MixedSizeBlocks_ShouldFall_BasedOnOccupiedCells()
    {
        var board = new BoardEntity();
        var bigBlock = BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1));
        var smallBlock = BlockEntity.CreateNormal(BlockColor.Blue, new Vector2I(1, 1));

        board.Place(new Vector2I(0, 0), bigBlock);
        board.Place(new Vector2I(1, 1), smallBlock);

        var result = FallService.Execute(board);

        var maxY = BoardEntity.Size.Y - 1;
        board.GetPositionOf(smallBlock).ShouldBe(new Vector2I(1, maxY));
        board.GetPositionOf(bigBlock).ShouldBe(new Vector2I(0, maxY - 1));
    }

    [Fact(DisplayName = "落下できないブロックのテスト")]
    public void AlreadyGroundedBlocks_ShouldNotMove()
    {
        var board = new BoardEntity();
        var block1 = BlockEntity.CreateNormal(BlockColor.Red);
        var block2 = BlockEntity.CreateNormal(BlockColor.Blue);

        var maxY = BoardEntity.Size.Y - 1;
        board.Place(new Vector2I(0, maxY), block1);
        board.Place(new Vector2I(0, maxY - 1), block2);

        var result = FallService.Execute(board);

        result.Movements.ShouldBeEmpty();
        result.HasFalled.ShouldBeFalse();

        // 座標が変わっていないことを確認
        board.GetPositionOf(block1).ShouldBe(new Vector2I(0, maxY));
        board.GetPositionOf(block2).ShouldBe(new Vector2I(0, maxY - 1));
    }

    [Fact(DisplayName = "空の盤面で実行してもエラーにならないテスト")]
    public void EmptyBoard_ShouldProduce_EmptyResult()
    {
        var board = new BoardEntity();

        var result = FallService.Execute(board);

        result.Movements.ShouldBeEmpty();
        result.HasFalled.ShouldBeFalse();
        board.Count().ShouldBe(0);
    }
}

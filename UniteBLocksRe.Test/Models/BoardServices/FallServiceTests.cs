using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Test.Models.BoardServices;

public class FallServiceTests
{
    [Fact(DisplayName = "基本的な落下テスト：1つのブロックが最下段まで落ちるか")]
    public void SingleBlock_ShouldFall_ToBottomMostRow()
    {
        // BoardEntityは固定サイズ (8, 14)
        var board = new BoardEntity();
        var block = BlockEntity.CreateNormal(BlockColor.Red);

        // (0,0)に配置
        board.Place(Vector2I.Zero, block);

        var result = FallService.Execute(board);

        result.Movements.Count.ShouldBe(1);
        result.Movements[0].From.ShouldBe(Vector2I.Zero);
        result.Movements[0].To.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));

        // 落下後の盤面で、そのブロックが最下段に存在することを確認
        var finalPos = board.GetPositionOf(block);
        finalPos.ShouldBe(new Vector2I(0, BoardEntity.Size.Y - 1));
    }

    [Fact(DisplayName = "縦に隣接したブロックの落下テスト：下のブロックから順に詰めて落ちるか")]
    public void VerticallyStackedBlocks_ShouldFall_SequentiallyToBottom()
    {
        var board = new BoardEntity();
        var upperBlock = BlockEntity.CreateNormal(BlockColor.Red);
        var lowerBlock = BlockEntity.CreateNormal(BlockColor.Blue);

        // (0,0) と (0,1) に配置
        board.Place(new Vector2I(0, 0), upperBlock);
        board.Place(new Vector2I(0, 1), lowerBlock);

        var result = FallService.Execute(board);

        result.Movements.Count.ShouldBe(2);

        var maxY = BoardEntity.Size.Y - 1;
        // 下にあったブロックが最下段へ
        board.GetPositionOf(lowerBlock).ShouldBe(new Vector2I(0, maxY));
        // 上にあったブロックがその一つ上へ
        board.GetPositionOf(upperBlock).ShouldBe(new Vector2I(0, maxY - 1));
    }

    [Fact(DisplayName = "形状の異なる隣接ブロックの落下テスト：複雑な形状でも正しく重なるか")]
    public void MixedSizeBlocks_ShouldFall_BasedOnOccupiedCells()
    {
        var board = new BoardEntity();
        // 2x1（横長）と 1x1 のブロックを用意
        var bigBlock = BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1));
        var smallBlock = BlockEntity.CreateNormal(BlockColor.Blue, new Vector2I(1, 1));

        // bigBlock(0,0)-(1,0), smallBlock(1,1) に配置
        board.Place(new Vector2I(0, 0), bigBlock);
        board.Place(new Vector2I(1, 1), smallBlock);

        var result = FallService.Execute(board);

        var maxY = BoardEntity.Size.Y - 1;
        // 先に x=1 の smallBlock が最下段 (1, maxY) へ
        board.GetPositionOf(smallBlock).ShouldBe(new Vector2I(1, maxY));
        // その上に 2x1 の bigBlock が乗る (0, maxY-1)
        board.GetPositionOf(bigBlock).ShouldBe(new Vector2I(0, maxY - 1));
    }

    [Fact(DisplayName = "落下できないブロックのテスト：既に詰まっている場合に変化しないか")]
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

        // 座標が変わっていないこと
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

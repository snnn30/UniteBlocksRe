using Shouldly;
using UniteBlocksRe.Domain;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Boards.Operations;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Test.Boards.Operations;

public class FallServiceTests
{
    [Fact(DisplayName = "基本的な落下テスト：1つのブロックが最下段まで落ちるか")]
    public void SingleBlock_ShouldFall_ToBottomMostRow()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var block = new NormalBlock(BlockColor.Red);
        board = board.Place(Vector2I.Zero, block);

        var result = FallService.Execute(board);
        result.Movements.Count.ShouldBe(1);

        // 落下後の盤面で、そのブロックが(0,4)に存在することを確認
        var finalPos = result.After.GetPositionOf(block);
        finalPos.ShouldBe(new Vector2I(0, 4));
    }

    [Fact(DisplayName = "縦に隣接したブロックの落下テスト：下のブロックから順に詰めて落ちるか")]
    public void VerticallyStackedBlocks_ShouldFall_SequentiallyToBottom()
    {
        // Arrange: (0,0)とそのすぐ下(0,1)に異なるインスタンスを配置
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var upperBlock = new NormalBlock(BlockColor.Red);
        var lowerBlock = new NormalBlock(BlockColor.Blue);
        board = board.Place(new Vector2I(0, 0), upperBlock).Place(new Vector2I(0, 1), lowerBlock);

        var result = FallService.Execute(board);
        result.Movements.Count.ShouldBe(2);

        // 下にあったブロックが最下段(0,4)へ
        result.After.GetPositionOf(lowerBlock).ShouldBe(new Vector2I(0, 4));
        // 上にあったブロックがその一つ上(0,3)へ
        result.After.GetPositionOf(upperBlock).ShouldBe(new Vector2I(0, 3));
    }

    [Fact(DisplayName = "形状の異なる隣接ブロックの落下テスト：複雑な形状でも正しく重なるか")]
    public void MixedSizeBlocks_ShouldFall_BasedOnOccupiedCells()
    {
        // 2x1（横長）と1x1のブロックを用意
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var bigBlock = new NormalBlock(new Vector2I(2, 1), BlockColor.Red);
        var smallBlock = new NormalBlock(new Vector2I(1, 1), BlockColor.Blue);
        board = board.Place(new Vector2I(0, 0), bigBlock).Place(new Vector2I(1, 1), smallBlock);

        var result = FallService.Execute(board);
        result.Movements.Count.ShouldBe(2);

        // 下にあるsmallBlock(x=1)が先に最下段(1,4)へ
        result.After.GetPositionOf(smallBlock).ShouldBe(new Vector2I(1, 4));
        result.After.GetPositionOf(bigBlock).ShouldBe(new Vector2I(0, 3));
    }

    [Fact(DisplayName = "落下できないブロックのテスト：既に詰まっている場合に変化しないか")]
    public void AlreadyGroundedBlocks_ShouldNotMove()
    {
        // Arrange: 最下段とその一つ上に配置
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var block1 = new NormalBlock(BlockColor.Red);
        var block2 = new NormalBlock(BlockColor.Blue);
        board = board.Place(new Vector2I(0, 4), block1).Place(new Vector2I(0, 3), block2);

        var result = FallService.Execute(board);
        result.Movements.ShouldBeEmpty();

        // 座標が変わっていないこと
        result.After.GetPositionOf(block1).ShouldBe(new Vector2I(0, 4));
        result.After.GetPositionOf(block2).ShouldBe(new Vector2I(0, 3));
    }

    [Fact(DisplayName = "空の盤面で実行してもエラーにならないテスト")]
    public void EmptyBoard_ShouldProduce_EmptyResult()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var result = FallService.Execute(board);

        result.Movements.ShouldBeEmpty();
        result.After.Grid.ShouldBeEmpty();
    }
}

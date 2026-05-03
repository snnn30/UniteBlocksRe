using Shouldly;
using UniteBlocksRe.Domain;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Boards.Operations;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Test.Boards.Operations;

public class UniteServiceTests
{
    [Fact(DisplayName = "縦に並んだ同色の巨大ブロック同士が合体して正方形になるテスト")]
    public void BasicUnite_TwoRectanglesIntoSquare()
    {
        // 2x1 が2つ縦に並んで 2x2 になる
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var redUpper = new NormalBlock(new Vector2I(2, 1), BlockColor.Red);
        var redLower = new NormalBlock(new Vector2I(2, 1), BlockColor.Red);

        board = board.Place(new(0, 0), redUpper);
        board = board.Place(new(0, 1), redLower);

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps.Count.ShouldBe(1);
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
        result.After.GetPositionOf(result.Steps[0].CreatedBlock).ShouldBe(new Vector2I(0, 0));
    }

    [Fact(DisplayName = "1x1のブロック4つが田の字に並んで2x2に合体するテスト")]
    public void FourSingleBlocks_IntoSquare()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var red = BlockColor.Red;

        board = board.Place(new(0, 0), new NormalBlock(red));
        board = board.Place(new(1, 0), new NormalBlock(red));
        board = board.Place(new(0, 1), new NormalBlock(red));
        board = board.Place(new(1, 1), new NormalBlock(red));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
    }

    [Fact(DisplayName = "隣接していても色が異なる場合は合体しないテスト")]
    public void DifferentColors_ShouldNotUnite()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        // 2x1 の赤と緑
        board = board.Place(new(0, 0), new NormalBlock(new Vector2I(2, 1), BlockColor.Red));
        board = board.Place(new(0, 1), new NormalBlock(new Vector2I(2, 1), BlockColor.Green));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeFalse();
    }

    [Fact(DisplayName = "範囲外にはみ出しているブロックが含まれる場合は合体しないテスト")]
    public void BlockProtrudingFromRect_ShouldNotUnite()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        board = board.Place(new(0, 0), new NormalBlock(new Vector2I(1, 2), BlockColor.Red));
        board = board.Place(new(1, 0), new NormalBlock(new Vector2I(1, 3), BlockColor.Red));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeFalse();
    }

    [Fact(DisplayName = "複雑な形状の組み合わせから可能な限り最大の長方形を作るテスト")]
    public void FindLargestPossibleRectangle()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var color = BlockColor.Red;

        // 3x2 の範囲を埋めるように配置
        board = board.Place(new(0, 0), new NormalBlock(new Vector2I(2, 1), color));
        board = board.Place(new(2, 0), new NormalBlock(new Vector2I(1, 2), color));
        board = board.Place(new(0, 1), new NormalBlock(new Vector2I(2, 1), color));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(3, 2));
    }

    [Fact(DisplayName = "盤面内の離れた場所で複数の合体が独立して発生するテスト")]
    public void MultipleIndependentUnites()
    {
        var board = new Board(new Vector2I(10, 10), new(2, 1));

        // 左上：赤の合体 (2x2)
        board = board.Place(new(0, 0), new NormalBlock(new Vector2I(2, 1), BlockColor.Red));
        board = board.Place(new(0, 1), new NormalBlock(new Vector2I(2, 1), BlockColor.Red));

        // 右下：青の合体 (2x3)
        board = board.Place(new(5, 7), new NormalBlock(new Vector2I(2, 1), BlockColor.Blue));
        board = board.Place(new(5, 5), new NormalBlock(new Vector2I(2, 2), BlockColor.Blue));

        var result = UniteService.Execute(board);

        result.Steps.Count.ShouldBe(2);
        // 赤の合体確認
        result.Steps.ShouldContain(s => s.CreatedBlock.Size == new Vector2I(2, 2));
        // 青の合体確認
        result.Steps.ShouldContain(s => s.CreatedBlock.Size == new Vector2I(2, 3));
    }

    [Fact(DisplayName = "凸型の配置で、有効な長方形が抽出され合体するテスト")]
    public void IrregularShape_ShouldFindValidRectangle()
    {
        /* 配置イメージ (R=Red):
           RRRR
           RR
        */
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var red = BlockColor.Red;

        // 1段目 (4つ)
        board = board.Place(new(0, 0), new NormalBlock(red));
        board = board.Place(new(1, 0), new NormalBlock(red));
        board = board.Place(new(2, 0), new NormalBlock(red));
        board = board.Place(new(3, 0), new NormalBlock(red));
        // 2段目 (2つ)
        board = board.Place(new(0, 1), new NormalBlock(red));
        board = board.Place(new(1, 1), new NormalBlock(red));

        var result = UniteService.Execute(board);

        // 合体が発生することを確認
        result.HasUnited.ShouldBeTrue();
        result.Steps[0].CreatedBlock.Size.Area.ShouldBeGreaterThanOrEqualTo(4);
    }
}

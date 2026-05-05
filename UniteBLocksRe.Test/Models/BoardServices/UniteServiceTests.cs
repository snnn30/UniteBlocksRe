using Godot;
using Shouldly;
using UniteBlocksRe.src.Models;
using UniteBlocksRe.src.Models.Block;
using UniteBlocksRe.src.Models.BoardServices;

namespace UniteBlocksRe.Test.Models.BoardServices;

public class UniteServiceTests
{
    [Fact(DisplayName = "縦に並んだ同色の巨大ブロック同士が合体して正方形になるテスト")]
    public void BasicUnite_TwoRectanglesIntoSquare()
    {
        var board = new BoardEntity();
        // 2x1 が2つ縦に並んで 2x2 になる
        var redUpper = BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1));
        var redLower = BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1));

        board.Place(new(0, 0), redUpper);
        board.Place(new(0, 1), redLower);

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps.Count.ShouldBe(1);

        var created = result.Steps[0].CreatedBlock;
        created.Size.ShouldBe(new Vector2I(2, 2));
        board.GetPositionOf(created).ShouldBe(new Vector2I(0, 0));

        // 元のブロックが消えていること
        board.Count().ShouldBe(1);
    }

    [Fact(DisplayName = "1x1のブロック4つが田の字に並んで2x2に合体するテスト")]
    public void FourSingleBlocks_IntoSquare()
    {
        var board = new BoardEntity();
        var red = BlockColor.Red;

        board.Place(new(0, 0), BlockEntity.CreateNormal(red));
        board.Place(new(1, 0), BlockEntity.CreateNormal(red));
        board.Place(new(0, 1), BlockEntity.CreateNormal(red));
        board.Place(new(1, 1), BlockEntity.CreateNormal(red));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(2, 2));
    }

    [Fact(DisplayName = "隣接していても色が異なる場合は合体しないテスト")]
    public void DifferentColors_ShouldNotUnite()
    {
        var board = new BoardEntity();
        // 2x1 の赤と緑
        board.Place(new(0, 0), BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1)));
        board.Place(new(0, 1), BlockEntity.CreateNormal(BlockColor.Green, new Vector2I(2, 1)));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeFalse();
        board.Count().ShouldBe(2);
    }

    [Fact(DisplayName = "範囲外にはみ出しているブロックが含まめる場合は合体しないテスト")]
    public void BlockProtrudingFromRect_ShouldNotUnite()
    {
        var board = new BoardEntity();
        // 1x2 と 1x3 を並べても、綺麗な長方形(2x2や2x3)にならないため合体しない
        board.Place(new(0, 0), BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(1, 2)));
        board.Place(new(1, 0), BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(1, 3)));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeFalse();
    }

    [Fact(DisplayName = "複雑な形状の組み合わせから可能な限り最大の長方形を作るテスト")]
    public void FindLargestPossibleRectangle()
    {
        var board = new BoardEntity();
        var color = BlockColor.Red;

        // 3x2 の範囲を埋めるように配置
        // (0,0)から2x1, (2,0)から1x2, (0,1)から2x1
        board.Place(new(0, 0), BlockEntity.CreateNormal(color, new Vector2I(2, 1)));
        board.Place(new(2, 0), BlockEntity.CreateNormal(color, new Vector2I(1, 2)));
        board.Place(new(0, 1), BlockEntity.CreateNormal(color, new Vector2I(2, 1)));

        var result = UniteService.Execute(board);

        result.HasUnited.ShouldBeTrue();
        result.Steps[0].CreatedBlock.Size.ShouldBe(new Vector2I(3, 2));
    }

    [Fact(DisplayName = "盤面内の離れた場所で複数の合体が独立して発生するテスト")]
    public void MultipleIndependentUnites()
    {
        var board = new BoardEntity();

        // 左上：赤の合体 (2x2)
        board.Place(new(0, 0), BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1)));
        board.Place(new(0, 1), BlockEntity.CreateNormal(BlockColor.Red, new Vector2I(2, 1)));

        // 右下：青の合体 (2x3)
        board.Place(new(5, 5), BlockEntity.CreateNormal(BlockColor.Blue, new Vector2I(2, 2)));
        board.Place(new(5, 7), BlockEntity.CreateNormal(BlockColor.Blue, new Vector2I(2, 1)));

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
           RRRR (4x1)
           RR   (2x1)
        */
        var board = new BoardEntity();
        var red = BlockColor.Red;

        // 1段目 (4つ)
        board.Place(new(0, 0), BlockEntity.CreateNormal(red));
        board.Place(new(1, 0), BlockEntity.CreateNormal(red));
        board.Place(new(2, 0), BlockEntity.CreateNormal(red));
        board.Place(new(3, 0), BlockEntity.CreateNormal(red));
        // 2段目 (2つ)
        board.Place(new(0, 1), BlockEntity.CreateNormal(red));
        board.Place(new(1, 1), BlockEntity.CreateNormal(red));

        var result = UniteService.Execute(board);

        // (0,0)地点から 2x2 の合体が発生することを確認
        result.HasUnited.ShouldBeTrue();
        result.Steps.Any(s => s.CreatedBlock.Size == new Vector2I(2, 2)).ShouldBeTrue();

        // 2x2合体後、右側に余った2つの1x1が残っているはず
        board.Count().ShouldBe(3); // (2x2) + (1x1) + (1x1)
    }
}

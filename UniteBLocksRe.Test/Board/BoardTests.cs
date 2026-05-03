using Shouldly;
using UniteBlocksRe.Domain;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Test.Boards;

public class BoardTests
{
    [Fact(DisplayName = "基本操作のテスト：配置と削除が正しく状態に反映されるか")]
    public void PlaceAndRemove_ShouldReflectInNewBoardState()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var block = new NormalBlock(BlockColor.Red);
        var pos = Vector2I.Zero;

        // ブロック配置
        board = board.Place(pos, block);
        board.Grid.GetValueOrDefault(pos).ShouldNotBeNull();

        // ブロック削除
        board = board.Remove(block);
        board.Grid.GetValueOrDefault(pos).ShouldBeNull();
    }

    [Fact(DisplayName = "画面端のテスト：盤面外への配置が正しく拒否されるか")]
    public void PlacementAtBoundaries_ShouldBeValidatedCorrectly()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var block1 = new NormalBlock(BlockColor.Red); // 1x1サイズ
        var block2 = new NormalBlock(new Vector2I(2, 1), BlockColor.Red); // 2x1サイズ

        board.CanPlace(board.Bounds, block1).ShouldBeFalse(); // 右下角の外側
        board.CanPlace(board.Bounds - Vector2I.One, block1).ShouldBeTrue(); // 右下角の内側
        board.CanPlace(Vector2I.Zero, block1).ShouldBeTrue(); // 左上角
        board.CanPlace(new Vector2I(-1, 0), block1).ShouldBeFalse(); // 左側の外側

        // 2x1ブロックの境界チェック
        board.CanPlace(board.Bounds - Vector2I.One, block2).ShouldBeFalse();
        board.CanPlace(board.Bounds - new Vector2I(2, 1), block2).ShouldBeTrue();
    }

    [Fact(DisplayName = "重なりのテスト：既存ブロックとの衝突判定が正しく機能するか")]
    public void OverlappingPlacement_ShouldBeRejectedAsConflict()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var block = new NormalBlock(new Vector2I(2, 2), BlockColor.Red);

        board = board.Place(Vector2I.Zero, block);

        board.CanPlace(Vector2I.One, block).ShouldBeFalse();
        board.CanPlace(new Vector2I(2, 2), block).ShouldBeTrue();
    }
}

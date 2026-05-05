using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Test.Models.BoardServices;

public class ObstaclePlaceServiceTests
{
    [Fact(DisplayName = "1つだけお邪魔を生成するテスト")]
    public void Place_SingleObstacle_ShouldBeAtBottom()
    {
        var board = new BoardEntity();

        var result = ObstaclePlaceService.Execute(board, 1);

        result.PlacedCount.ShouldBe(1);
        result.Colmuns.Count.ShouldBe(1);

        // 座標と型を検証
        var firstCol = result.Colmuns.First();
        var placed = firstCol.Value.Blocks[0];

        // デフォルトの盤面は 8x14 なので、Y=13 が最下段
        placed.position.Y.ShouldBe(BoardEntity.Size.Y - 1);
        placed.block.Type.ShouldBe(BlockType.Obstacle);

        // 盤面状態が更新されているか
        board[placed.position].ShouldBe(placed.block);
    }

    [Fact(DisplayName = "1段分（横幅いっぱい）生成するテスト")]
    public void Place_FullRow_ShouldFillBottomLine()
    {
        var board = new BoardEntity();
        var width = BoardEntity.Size.X;

        var result = ObstaclePlaceService.Execute(board, width);

        result.PlacedCount.ShouldBe(width);
        result.Colmuns.Count.ShouldBe(width);

        foreach (var colResult in result.Colmuns.Values)
        {
            colResult.Blocks.Count.ShouldBe(1);
            colResult.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
        }
    }

    [Fact(DisplayName = "既存のブロックがある時に避けて配置されるテスト")]
    public void Place_WithExistingBlocks_ShouldAdjustAltitude()
    {
        var board = new BoardEntity();
        var obstaclePos = new Vector2I(3, BoardEntity.Size.Y - 1); // 列3の最下段

        // 既存ブロックを配置
        board.Place(obstaclePos, BlockEntity.CreateNormal(BlockColor.Red));

        // 1行分(8個)要求
        var result = ObstaclePlaceService.Execute(board, BoardEntity.Size.X);

        result.PlacedCount.ShouldBe(BoardEntity.Size.X);

        // 列3は既存ブロックがあるため、その上 (13 - 1 = 12) に置かれる
        var col3 = result.Colmuns[3];
        col3.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 2);

        // 他の空いている列（例: 列0）は最下段 (13)
        result.Colmuns[0].Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
    }

    [Fact(DisplayName = "1列あたりの最大段数制限が守られるテスト")]
    public void Place_RespectsMaxPerColumn()
    {
        var board = new BoardEntity();
        var maxPerCol = ObstaclePlaceService.MaxPerColumn;
        var requestCount = 100; // 過剰なリクエスト

        var result = ObstaclePlaceService.Execute(board, requestCount);

        // 8列 * 4個 = 32個が上限のはず
        result.PlacedCount.ShouldBe(BoardEntity.Size.X * maxPerCol);

        foreach (var col in result.Colmuns.Values)
        {
            col.Blocks.Count.ShouldBe(maxPerCol);
        }
    }

    [Fact(DisplayName = "端数がランダムに分配されるテスト")]
    public void Place_DistributesRemainderRandomly()
    {
        var board = new BoardEntity();
        // 全列に1段ずつ + どこか2列に2段目 (計 8 + 2 = 10個)
        var requestCount = BoardEntity.Size.X + 2;

        var result = ObstaclePlaceService.Execute(board, requestCount);

        result.PlacedCount.ShouldBe(requestCount);

        // 2段になった列が2つ、1段の列が6つあるはず
        result.Colmuns.Values.Count(c => c.Blocks.Count == 2).ShouldBe(2);
        result.Colmuns.Values.Count(c => c.Blocks.Count == 1).ShouldBe(6);
    }

    [Fact(DisplayName = "盤面に空きがない時に配置されずエラーにもならないテスト")]
    public void Place_WhenBoardIsFull_ShouldHandleGracefully()
    {
        var board = new BoardEntity();

        // 全マスを埋める
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                board.Place(new(x, y), BlockEntity.CreateNormal(BlockColor.Red));
            }
        }

        var result = ObstaclePlaceService.Execute(board, 10);

        result.Placed.ShouldBeFalse();
        result.PlacedCount.ShouldBe(0);
    }

    [Fact(DisplayName = "一部の列に空きがない時に他の列へ優先して配置されるテスト")]
    public void Place_ShouldUseAvailableColumns_WhenSomeAreBlocked()
    {
        var board = new BoardEntity();

        // 列3を完全に埋める
        for (int y = 0; y < BoardEntity.Size.Y; y++)
        {
            board.Place(new(3, y), BlockEntity.CreateNormal(BlockColor.Blue));
        }

        // 5個配置を試みる
        var result = ObstaclePlaceService.Execute(board, 5);

        result.PlacedCount.ShouldBe(5);

        // 列3は既に埋まっているので、配置結果に含まれていないはず
        result.Colmuns.ContainsKey(3).ShouldBeFalse();

        // 他の列に正しく配置されていること
        result.PlacedCount.ShouldBe(5);
    }
}

using Shouldly;
using UniteBlocksRe.Domain;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Boards;
using UniteBlocksRe.Domain.Boards.Operations;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Test.Boards.Operations;

public class ObstaclePlaceServiceTests
{
    [Fact(DisplayName = "1つだけお邪魔を生成するテスト")]
    public void Place_SingleObstacle_ShouldBeAtBottom()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));

        var result = ObstaclePlaceService.Execute(board, 1);

        result.PlacedCount.ShouldBe(1);
        result.Colmuns.Count.ShouldBe(1);

        // 座標と型を検証
        var (colIndex, colResult) = result.Colmuns.First();
        var placed = colResult.Blocks[0];

        placed.position.Y.ShouldBe(board.Bounds.Y - 1); // 最下段
        placed.block.ShouldBeOfType<ObstacleBlock>();

        // 盤面状態が更新されているか（AfterのGridに物理的に存在するか）
        result.After.Grid[placed.position].ShouldBe(placed.block);
    }

    [Fact(DisplayName = "1段分（横幅いっぱい）生成するテスト")]
    public void Place_FullRow_ShouldFillBottomLine()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));

        var result = ObstaclePlaceService.Execute(board, board.Bounds.X);

        result.PlacedCount.ShouldBe(board.Bounds.X);
        result.Colmuns.Count.ShouldBe(board.Bounds.X);

        foreach (var colResult in result.Colmuns.Values)
        {
            colResult.Blocks.Count.ShouldBe(1);
            colResult.Blocks[0].position.Y.ShouldBe(board.Bounds.Y - 1);
        }
    }

    [Fact(DisplayName = "既存のブロックがある時に避けて配置されるテスト")]
    public void Place_WithExistingBlocks_ShouldAdjustAltitude()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var obstaclePos = new Vector2I(3, 4); // 列3の最下段
        board = board.Place(obstaclePos, new NormalBlock(BlockColor.Red));

        var result = ObstaclePlaceService.Execute(board, board.Bounds.X);

        result.PlacedCount.ShouldBe(board.Bounds.X);

        // 列3だけは既存ブロックの上 (4 - 1 = 3) に置かれるはず
        var col3 = result.Colmuns[3];
        col3.Blocks[0].position.Y.ShouldBe(3);

        // 他の列（例: 列0）は最下段
        result.Colmuns[0].Blocks[0].position.Y.ShouldBe(4);
    }

    [Fact(DisplayName = "1列あたりの最大段数制限が守られるテスト")]
    public void Place_RespectsMaxPerColumn()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var maxPerCol = ObstaclePlaceService.MaxPerColumn;
        var requestCount = 100;

        var result = ObstaclePlaceService.Execute(board, requestCount);

        // 5列 * 2個 = 10個
        result.PlacedCount.ShouldBe(board.Bounds.X * maxPerCol);

        foreach (var col in result.Colmuns.Values)
        {
            col.Blocks.Count.ShouldBeLessThanOrEqualTo(maxPerCol);
        }
    }

    [Fact(DisplayName = "端数がランダムに分配されるテスト")]
    public void Place_DistributesRemainderRandomly()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        // 5列に対して7個生成（全員1段 + どこか2列が2段）
        var requestCount = board.Bounds.X + 2;

        var result = ObstaclePlaceService.Execute(board, requestCount);

        result.PlacedCount.ShouldBe(requestCount);

        // 2段になった列が2つ、1段の列が3つあるはず
        result.Colmuns.Values.Count(c => c.Blocks.Count == 2).ShouldBe(2);
        result.Colmuns.Values.Count(c => c.Blocks.Count == 1).ShouldBe(3);
    }

    [Fact(DisplayName = "盤面に空きがない時に配置されずエラーにもならないテスト")]
    public void Place_WhenBoardIsFull_ShouldHandleGracefully()
    {
        // 2x2を埋める
        var board = new Board(new Vector2I(2, 2), new(0, 0));
        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                board = board.Place(new(x, y), new NormalBlock(BlockColor.Red));
            }
        }

        var result = ObstaclePlaceService.Execute(board, 10);

        result.Placed.ShouldBeFalse();
        result.PlacedCount.ShouldBe(0);
        result.After.ShouldBe(board); // インスタンスが同一であることを検証
    }

    [Fact(DisplayName = "一部の列に空きがない時に他の列へ優先して配置されるテスト")]
    public void Place_ShouldUseAvailableColumns_WhenSomeAreBlocked()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        // 列3を上まで埋める
        for (int y = 0; y < board.Bounds.Y; y++)
        {
            board = board.Place(new(3, y), new NormalBlock(BlockColor.Blue));
        }

        // 5個配置を試みる
        var result = ObstaclePlaceService.Execute(board, 5);

        result.PlacedCount.ShouldBe(5);
        // 列3は既に埋まっているので、結果に含まれていないはず
        result.Colmuns.ContainsKey(3).ShouldBeFalse();
        // その分、他の列が重複して（2段）埋まっているはず
        result.Colmuns.Values.Any(c => c.Blocks.Count > 1).ShouldBeTrue();
    }
}

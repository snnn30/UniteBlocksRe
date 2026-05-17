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

        var firstCol = result.Colmuns.First();
        var placed = firstCol.Value.Blocks[0];

        placed.position.Y.ShouldBe(BoardEntity.Size.Y - 1);
        placed.block.Type.ShouldBe(BlockType.Obstacle);

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
        var obstaclePos = new Vector2I(3, BoardEntity.Size.Y - 1);

        board.Place(obstaclePos, BlockEntity.CreateNormal(BlockColor.Red));

        var result = ObstaclePlaceService.Execute(board, BoardEntity.Size.X);

        result.PlacedCount.ShouldBe(BoardEntity.Size.X);

        var col3 = result.Colmuns[3];
        col3.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 2);

        result.Colmuns[0].Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
    }

    [Fact(DisplayName = "1列あたりの最大段数制限が守られるテスト")]
    public void Place_RespectsMaxPerColumn()
    {
        var board = new BoardEntity();
        var maxPerCol = ObstaclePlaceService.MaxPerColumn;
        var requestCount = 100;

        var result = ObstaclePlaceService.Execute(board, requestCount);

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
        // 全列に1段ずつ + どこか2列に2段目
        var requestCount = BoardEntity.Size.X + 2;

        var result = ObstaclePlaceService.Execute(board, requestCount);

        result.PlacedCount.ShouldBe(requestCount);

        // 2段になった列が2つだけあるはず
        result.Colmuns.Values.Count(c => c.Blocks.Count == 2).ShouldBe(2);
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
        for (var y = 0; y < BoardEntity.Size.Y; y++)
        {
            board.Place(new(3, y), BlockEntity.CreateNormal(BlockColor.Blue));
        }

        var result = ObstaclePlaceService.Execute(board, 5);

        result.PlacedCount.ShouldBe(5);
        result.Colmuns.ContainsKey(3).ShouldBeFalse();
        result.PlacedCount.ShouldBe(5);
    }
}

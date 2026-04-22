using Shouldly;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.Services;
using UniteBlocksRe.src.Models.ValueObjects;

namespace UniteBlocksRe.Test.Models.Services;

public class BoardObstaclePlacerTests
{
    [Fact(DisplayName = "1つだけお邪魔を生成するテスト")]
    public void Test1()
    {
        var board = new BoardEntity();

        var result = BoardObstaclePlacer.Place(board, 1, 5);

        result.PlacedCount.ShouldBe(1);
        var colResult = result.Colmuns.Values.First();
        colResult.Blocks.Count.ShouldBe(1);
        colResult.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
    }

    [Fact(DisplayName = "1段分生成するテスト")]
    public void Test2()
    {
        var board = new BoardEntity();

        var result = BoardObstaclePlacer.Place(board, BoardEntity.Size.X, 5);

        result.Colmuns.Count.ShouldBe(BoardEntity.Size.X);
        result.PlacedCount.ShouldBe(BoardEntity.Size.X);
        foreach (var col in result.Colmuns.Values)
        {
            col.Blocks.Count.ShouldBe(1);
            col.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
        }
    }

    [Fact(DisplayName = "既存のブロックがある時に1段分生成するテスト")]
    public void Test3()
    {
        var board = new BoardEntity();
        board.TrySetBlock(new(3, 6), new BlockEntity(BlockColor.Red));

        var result = BoardObstaclePlacer.Place(board, BoardEntity.Size.X, 5);

        result.Colmuns.Count.ShouldBe(BoardEntity.Size.X);
        result.PlacedCount.ShouldBe(BoardEntity.Size.X);
        foreach (var (col, colResult) in result.Colmuns)
        {
            colResult.Blocks.Count.ShouldBe(1);

            if (col == 3)
            {
                colResult.Blocks[0].position.Y.ShouldBe(5);
            }
            else
            {
                colResult.Blocks[0].position.Y.ShouldBe(BoardEntity.Size.Y - 1);
            }
        }
    }

    [Fact(DisplayName = "最大段数制限が守られるテスト")]
    public void Test4()
    {
        var board = new BoardEntity();

        var maxPerCol = 2;
        var result = BoardObstaclePlacer.Place(board, 100, maxPerCol);

        foreach (var col in result.Colmuns.Values)
        {
            col.Blocks.Count.ShouldBeLessThanOrEqualTo(maxPerCol);
        }

        result.Colmuns.Values.Sum(c => c.Blocks.Count).ShouldBe(BoardEntity.Size.X * maxPerCol);
    }

    [Fact(DisplayName = "端数がランダムに分配されるテスト")]
    public void Test5()
    {
        var board = new BoardEntity();

        // 10個生成（1段 8個 + 端数 2個）
        var result = BoardObstaclePlacer.Place(board, 10, 5);

        result.Colmuns.Values.Sum(c => c.Blocks.Count).ShouldBe(10);

        result.Colmuns.Values.Count(c => c.Blocks.Count == 2).ShouldBe(2);
        result.Colmuns.Values.Count(c => c.Blocks.Count == 1).ShouldBe(6);
    }

    [Fact(DisplayName = "空きなしでエラーにならないテスト")]
    public void Test6()
    {
        var board = new BoardEntity();

        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                board.TrySetBlock(new(x, y), new BlockEntity(BlockColor.Red));
            }
        }

        var result = BoardObstaclePlacer.Place(board, 10, 5);
        result.Placed.ShouldBeFalse();
    }

    [Fact(DisplayName = "一部空きがない時にその代わり他の列に配置されるか")]
    public void Test7()
    {
        var board = new BoardEntity();

        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            board.TrySetBlock(new(x, 4), new BlockEntity(BlockColor.Red));
        }
        for (var y = 0; y < 4; y++)
        {
            board.TrySetBlock(new(3, y), new BlockEntity(BlockColor.Red));
        }

        // 2段が3列 6 3段が4列 12
        var result = BoardObstaclePlacer.Place(board, 18, 5);

        result.Colmuns.Values.Count(c => c.Blocks.Count == 2).ShouldBe(3);
        result.Colmuns.Values.Count(c => c.Blocks.Count == 3).ShouldBe(4);
    }
}

using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Test.Models.BoardServices;

public class ExplodeServiceTests
{
    [Fact(DisplayName = "ボムが爆発し、その下のブロックが連鎖するテスト")]
    public void BombExplosion_ShouldTrigger_BlockBelow()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.CreateBomb();
        var block = BlockEntity.CreateNormal(BlockColor.Red);

        board.Place(new(0, 0), bomb);
        board.Place(new(0, 1), block);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(2);
        result.Steps[0].ExplodedBlocks.ShouldContain(bomb);
        result.Steps[1].ExplodedBlocks.ShouldContain(block);
    }

    [Fact(DisplayName = "ノーマルブロックが隣接する同色ブロックを連鎖爆発させるテスト")]
    public void NormalBlock_ShouldChain_AdjacentSameColorBlocks_Sequentially()
    {
        var board = new BoardEntity();
        var bomb = BlockEntity.CreateBomb();
        var red1 = BlockEntity.CreateNormal(BlockColor.Red);
        var red2 = BlockEntity.CreateNormal(BlockColor.Red);
        var red3 = BlockEntity.CreateNormal(BlockColor.Red);

        board.Place(new(0, 0), bomb);
        board.Place(new(0, 1), red1);
        board.Place(new(1, 1), red2);
        board.Place(new(1, 2), red3);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(4);
        result.Steps[1].ExplodedBlocks.ShouldContain(red1);
        result.Steps[2].ExplodedBlocks.ShouldContain(red2);
        result.Steps[3].ExplodedBlocks.ShouldContain(red3);
    }

    [Fact(DisplayName = "隣接していない同色ブロックは連鎖しないテスト")]
    public void NormalBlock_ShouldNotChain_NonAdjacentSameColorBlocks()
    {
        var board = new BoardEntity();
        var red1Pos = new Vector2I(0, 1);
        var red2Pos = new Vector2I(2, 1); // red1とは隣接していない

        var bomb = BlockEntity.CreateBomb();
        var red1 = BlockEntity.CreateNormal(BlockColor.Red);
        var red2 = BlockEntity.CreateNormal(BlockColor.Red);

        board.Place(new(0, 0), bomb);
        board.Place(red1Pos, red1);
        board.Place(red2Pos, red2);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(2);
        result.Steps[1].ExplodedBlocks.ShouldContain(red1);

        board[red2Pos.X, red2Pos.Y].ShouldBe(red2);
    }

    [Fact(DisplayName = "異なる色のブロックは連鎖を止めるテスト")]
    public void NormalBlock_ShouldStopChain_WhenAdjacentColorIsDifferent()
    {
        var board = new BoardEntity();
        var redPos = new Vector2I(0, 1);
        var bluePos = new Vector2I(1, 1);

        var bomb = BlockEntity.CreateBomb();
        var redBlock = BlockEntity.CreateNormal(BlockColor.Red);
        var blueBlock = BlockEntity.CreateNormal(BlockColor.Blue);

        board.Place(new(0, 0), bomb);
        board.Place(redPos, redBlock);
        board.Place(bluePos, blueBlock);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(2);
        result.Steps[1].ExplodedBlocks.ShouldContain(redBlock);

        board[bluePos.X, bluePos.Y].ShouldBe(blueBlock);
    }

    [Fact(DisplayName = "1つのブロックから複数の同色ブロックへ同時に連鎖するテスト")]
    public void SingleBlock_ShouldTrigger_MultipleAdjacentSameColorBlocks_Simultaneously()
    {
        var board = new BoardEntity();
        var bombPos = new Vector2I(1, 0);
        var centerPos = new Vector2I(1, 1);
        var leftPos = new Vector2I(0, 1);
        var rightPos = new Vector2I(2, 1);

        var bomb = BlockEntity.CreateBomb();
        var centerRed = BlockEntity.CreateNormal(BlockColor.Red);
        var leftRed = BlockEntity.CreateNormal(BlockColor.Red);
        var rightRed = BlockEntity.CreateNormal(BlockColor.Red);

        board.Place(bombPos, bomb);
        board.Place(centerPos, centerRed);
        board.Place(leftPos, leftRed);
        board.Place(rightPos, rightRed);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(3);
        result.Steps[2].ExplodedBlocks.Count.ShouldBe(2);
        result.Steps[2].ExplodedBlocks.ShouldContain(leftRed);
        result.Steps[2].ExplodedBlocks.ShouldContain(rightRed);
    }

    [Fact(DisplayName = "複数のボムが同時に存在し、それぞれが起点となって連鎖するテスト")]
    public void MultipleBombs_ShouldExplode_SimultaneouslyAsStartingPoints()
    {
        var board = new BoardEntity();
        var bomb1 = BlockEntity.CreateBomb();
        var red1 = BlockEntity.CreateNormal(BlockColor.Red);
        var bomb2 = BlockEntity.CreateBomb();
        var blue1 = BlockEntity.CreateNormal(BlockColor.Blue);

        board.Place(new(0, 0), bomb1);
        board.Place(new(0, 1), red1);
        board.Place(new(4, 0), bomb2);
        board.Place(new(4, 1), blue1);

        var result = ExplodeService.Execute(board);

        result.Steps[0].ExplodedBlocks.Count.ShouldBe(2);

        result.Steps[1].ExplodedBlocks.Count.ShouldBe(2);
        result.Steps[1].ExplodedBlocks.ShouldContain(red1);
        result.Steps[1].ExplodedBlocks.ShouldContain(blue1);
    }

    [Fact(DisplayName = "爆発の循環参照で無限ループしないテスト")]
    public void CircularInduction_ShouldNotCause_InfiniteLoop()
    {
        var board = new BoardEntity();
        var red1 = BlockEntity.CreateNormal(BlockColor.Red);
        var red2 = BlockEntity.CreateNormal(BlockColor.Red);

        board.Place(new(1, 0), BlockEntity.CreateBomb());
        board.Place(new(1, 1), red1);
        board.Place(new(2, 1), red2);

        var result = ExplodeService.Execute(board);

        board.Count().ShouldBe(0);
        result.Steps.SelectMany(s => s.ExplodedBlocks).Count().ShouldBe(3);
    }

    [Fact(DisplayName = "ボムの下が場外の場合でもエラーにならないテスト")]
    public void BombAtBoardEdge_ShouldHandle_OutOfBoundsInductionGracefully()
    {
        var board = new BoardEntity();
        var bottomPos = new Vector2I(1, BoardEntity.Size.Y - 1);

        board.Place(bottomPos, BlockEntity.CreateBomb());

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(1);
        board.Count().ShouldBe(0);
    }

    [Fact(DisplayName = "ノーマルブロックの爆発が隣接するお邪魔ブロックを誘発するテスト")]
    public void NormalBlock_ShouldTrigger_AdjacentObstacleBlocks()
    {
        var board = new BoardEntity();

        var bomb = BlockEntity.CreateBomb();
        var redBlock = BlockEntity.CreateNormal(BlockColor.Red);
        var obstacle = BlockEntity.CreateObstacle();

        board.Place(new(0, 0), bomb);
        board.Place(new(0, 1), redBlock);
        board.Place(new(1, 1), obstacle);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(3);

        result.Steps[0].ExplodedBlocks.ShouldContain(bomb);
        result.Steps[1].ExplodedBlocks.ShouldContain(redBlock);
        result.Steps[2].ExplodedBlocks.ShouldContain(obstacle);

        board.Count().ShouldBe(0);
    }
}

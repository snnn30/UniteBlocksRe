using Shouldly;
using UniteBlocksRe.Domain;
using UniteBlocksRe.Domain.Blocks;
using UniteBlocksRe.Domain.Boards.Operations;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Test.Boards.Operations;

public class ExplodeServiceTests
{
    [Fact(DisplayName = "ボムが爆発し、その下のブロックが連鎖するテスト")]
    public void BombExplosion_ShouldTrigger_BlockBelow()
    {
        var board = new Board(new Vector2I(1, 3), new(0, 0));
        var bomb = new BombBlock();
        var block = new NormalBlock(BlockColor.Red);
        board = board.Place(new(0, 0), bomb);
        board = board.Place(new(0, 1), block);

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(2);
        // 0番目のステップで (0,0) が爆発したか
        result.Steps[0].ExplodedBlocks.ShouldContain(b => b == bomb);
        // 1番目のステップで (0,1) が爆発したか
        result.Steps[1].ExplodedBlocks.ShouldContain(b => b == block);
    }

    [Fact(DisplayName = "ノーマルブロックが隣接する同色ブロックを連鎖爆発させるテスト")]
    public void NormalBlock_ShouldChain_AdjacentSameColorBlocks_Sequentially()
    {
        var board = new Board(new Vector2I(3, 3), new(2, 1));
        board = board.Place(new(0, 0), new BombBlock());
        board = board.Place(new(0, 1), new NormalBlock(BlockColor.Red));
        board = board.Place(new(1, 1), new NormalBlock(BlockColor.Red));
        board = board.Place(new(1, 2), new NormalBlock(BlockColor.Red));

        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(4);
        result
            .Steps[1]
            .ExplodedBlocks.ShouldContain(b =>
                result.Before.GetPositionOf(b) == new Vector2I(0, 1)
            );
        result
            .Steps[2]
            .ExplodedBlocks.ShouldContain(b =>
                result.Before.GetPositionOf(b) == new Vector2I(1, 1)
            );
        result
            .Steps[3]
            .ExplodedBlocks.ShouldContain(b =>
                result.Before.GetPositionOf(b) == new Vector2I(1, 2)
            );
    }

    [Fact(DisplayName = "隣接していない同色ブロックは連鎖しないテスト")]
    public void NormalBlock_ShouldNotChain_NonAdjacentSameColorBlocks()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var red1Pos = new Vector2I(0, 1);
        var red2Pos = new Vector2I(2, 1); // red1とは隣接していない

        board = board.Place(new(0, 0), new BombBlock());
        board = board.Place(red1Pos, new NormalBlock(BlockColor.Red));
        board = board.Place(red2Pos, new NormalBlock(BlockColor.Red));

        var result = ExplodeService.Execute(board);

        // ボム -> red1 で連鎖が止まるはず
        result.Steps.Count.ShouldBe(2);
        result
            .Steps[1]
            .ExplodedBlocks.ShouldContain(b => result.Before.GetPositionOf(b) == red1Pos);

        // red2 は盤面に残っていることを検証
        result.After.Grid.ContainsKey(red2Pos).ShouldBeTrue();
    }

    [Fact(DisplayName = "異なる色のブロックは連鎖を止めるテスト")]
    public void NormalBlock_ShouldStopChain_WhenAdjacentColorIsDifferent()
    {
        var board = new Board(new Vector2I(3, 3), new(2, 1));
        var redPos = new Vector2I(0, 1);
        var bluePos = new Vector2I(1, 1); // redに隣接しているが色が違う
        var redBlock = new NormalBlock(BlockColor.Red);
        var blueBlock = new NormalBlock(BlockColor.Blue);

        board = board.Place(new(0, 0), new BombBlock());
        board = board.Place(redPos, redBlock);
        board = board.Place(bluePos, blueBlock);

        var result = ExplodeService.Execute(board);

        // ボム -> red で止まるはず
        result.Steps.Count.ShouldBe(2);
        result.Steps[1].ExplodedBlocks.ShouldContain(b => b == redBlock);

        // blue は爆発せず残っているはず
        result.After.Grid.ContainsValue(blueBlock).ShouldBeTrue();
    }

    [Fact(DisplayName = "1つのブロックから複数の同色ブロックへ同時に連鎖するテスト")]
    public void SingleBlock_ShouldTrigger_MultipleAdjacentSameColorBlocks_Simultaneously()
    {
        var board = new Board(new Vector2I(3, 3), new(2, 1));
        var bombPos = new Vector2I(1, 0);
        var centerPos = new Vector2I(1, 1);
        var leftPos = new Vector2I(0, 1);
        var rightPos = new Vector2I(2, 1);

        board = board.Place(bombPos, new BombBlock());
        board = board.Place(centerPos, new NormalBlock(BlockColor.Red));
        board = board.Place(leftPos, new NormalBlock(BlockColor.Red));
        board = board.Place(rightPos, new NormalBlock(BlockColor.Red));

        var result = ExplodeService.Execute(board);

        // Step 0: Bomb
        // Step 1: Center Red
        // Step 2: Left & Right Reds (同時に誘発)
        result.Steps.Count.ShouldBe(3);
        result.Steps[2].ExplodedBlocks.Count.ShouldBe(2);

        // 座標を使って検証（Blockが位置を持たない設計に準拠）
        var step2Positions = result
            .Steps[2]
            .ExplodedBlocks.Select(b => board.GetPositionOf(b))
            .ToList();

        step2Positions.ShouldContain(leftPos);
        step2Positions.ShouldContain(rightPos);
    }

    [Fact(DisplayName = "複数のボムが同時に存在し、それぞれが起点となって連鎖するテスト")]
    public void MultipleBombs_ShouldExplode_SimultaneouslyAsStartingPoints()
    {
        var board = new Board(new Vector2I(5, 5), new(2, 1));
        var bomb1Pos = new Vector2I(0, 0);
        var red1Pos = new Vector2I(0, 1);
        var bomb2Pos = new Vector2I(4, 0);
        var blue1Pos = new Vector2I(4, 1);

        board = board.Place(bomb1Pos, new BombBlock());
        board = board.Place(red1Pos, new NormalBlock(BlockColor.Red));
        board = board.Place(bomb2Pos, new BombBlock());
        board = board.Place(blue1Pos, new NormalBlock(BlockColor.Blue));

        var result = ExplodeService.Execute(board);

        // Step 0 で両方のボムが同時に爆発
        result.Steps[0].ExplodedBlocks.Count.ShouldBe(2);

        // Step 1 でそれぞれのボムの下にあるブロックが同時に誘発
        result.Steps[1].ExplodedBlocks.Count.ShouldBe(2);

        var step1Positions = result
            .Steps[1]
            .ExplodedBlocks.Select(b => board.GetPositionOf(b))
            .ToList();
        step1Positions.ShouldContain(red1Pos);
        step1Positions.ShouldContain(blue1Pos);
    }

    [Fact(DisplayName = "爆発の循環参照で無限ループしないテスト")]
    public void CircularInduction_ShouldNotCause_InfiniteLoop()
    {
        var board = new Board(new Vector2I(3, 3), new(2, 1));
        var red1Pos = new Vector2I(1, 1);
        var red2Pos = new Vector2I(2, 1);

        board = board.Place(new(1, 0), new BombBlock());
        board = board.Place(red1Pos, new NormalBlock(BlockColor.Red));
        board = board.Place(red2Pos, new NormalBlock(BlockColor.Red));

        // 実行してフリーズしないこと
        var result = ExplodeService.Execute(board);

        result.After.Grid.Count.ShouldBe(0);
        // 合計爆発数が 3 (Bomb + Red1 + Red2) であること
        result.Steps.SelectMany(s => s.ExplodedBlocks).Count().ShouldBe(3);
    }

    [Fact(DisplayName = "ボムの下が場外の場合でもエラーにならないテスト")]
    public void BombAtBoardEdge_ShouldHandle_OutOfBoundsInductionGracefully()
    {
        var board = new Board(new Vector2I(3, 3), new(2, 1));
        var bottomPos = new Vector2I(1, 2); // 最下段

        board = board.Place(bottomPos, new BombBlock());

        // GetValueOrDefault が null を返し、ToEnumerable がそれを空リストにするため安全
        var result = ExplodeService.Execute(board);

        result.Steps.Count.ShouldBe(1);
        result.After.Grid.Count.ShouldBe(0);
    }
}

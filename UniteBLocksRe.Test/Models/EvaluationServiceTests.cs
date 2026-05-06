using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.Evaluation;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;

namespace UniteBlocksRe.Test.Models;

public class EvaluationServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly EvaluationWeight _weights;

    public EvaluationServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _weights = new DefaultEvaluationWeight();
    }

    #region Helpers
    private OperatingBlocksEntity SpawnRedPair(BoardEntity board)
    {
        // デフォルト位置でのスポーン
        var (success, entity) = OperatingBlocksEntity.TrySpawnDouble(
            BlockEntity.CreateNormal(BlockColor.Red),
            BlockEntity.CreateNormal(BlockColor.Red),
            board
        );
        return entity;
    }

    private OperatingBlocksEntity SpawnBomb(BoardEntity board)
    {
        var (success, entity) = OperatingBlocksEntity.TrySpawnSingle(
            BlockEntity.CreateBomb(),
            board
        );
        return entity;
    }

    // Evaluateメソッドをテストするために最小限のSimulationResultを生成する
    private SimulationResult CreateSimResult(BoardEntity b) =>
        new(
            b,
            new ProcessResult(new List<IProcessStep>()),
            Vector2I.Zero,
            Vector2I.Zero,
            new List<StepInfo>()
        );
    #endregion

    [Fact(DisplayName = "Test1: 低く積むことを優先するべき")]
    public void Test1_PrioritizeLowerHeight()
    {
        var board = new BoardEntity();
        // 中央に障害物を置いて高くする
        board.Place(new(3, BoardEntity.Size.Y - 1), BlockEntity.CreateObstacle());
        board.Place(new(3, BoardEntity.Size.Y - 2), BlockEntity.CreateObstacle());

        var (_, sim) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );

        // 一番下に配置されているか
        sim.ParentDestination.Y.ShouldBe(BoardEntity.Size.Y - 1);
        sim.ChildDestination.Y.ShouldBe(BoardEntity.Size.Y - 1);
    }

    [Fact(DisplayName = "Test2: すでに同色のブロックがあるとき、隣接を狙うべき")]
    public void Test2_PrioritizeSameColorAdjacency()
    {
        var board = new BoardEntity();
        var targetPos = new Vector2I(BoardEntity.Size.X - 1, BoardEntity.Size.Y - 1);
        var block = BlockEntity.CreateNormal(BlockColor.Red);
        board.Place(targetPos, block);

        var (_, sim) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );

        // 赤ペアのどちらかが、既存の赤の隣（左隣）に配置されているか検証
        var placedPositions = new[] { sim.ParentDestination, sim.ChildDestination };
        sim.Board.GetAdjacentBlocks(block).Count().ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Test3: ゲームオーバーを確実に回避するべき")]
    public void Test3_AvoidGameOver()
    {
        var board = new BoardEntity();
        // スポーン地点の直下以外をすべて埋める
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = BoardEntity.SpawnPosition.Y + 2; y < BoardEntity.Size.Y; y++)
            {
                if (x == BoardEntity.SpawnPosition.X)
                {
                    continue;
                }

                board.Place(new(x, y), BlockEntity.CreateObstacle());
            }
        }

        var (eval, _) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );

        // 置ける場所があるなら回避されているはず
        eval.Scores[EvaluationCriterion.CantSpawnPenalty].ShouldBe(0);
    }

    [Fact(DisplayName = "Test4: 複数の塊が存在する時、より面積が大きくなる合体を優先するべき")]
    public void Test4_PreferLargerMerge()
    {
        var board = new BoardEntity();
        board.Place(
            new(0, BoardEntity.Size.Y - 2),
            BlockEntity.CreateNormal(BlockColor.Red, new(2, 2))
        );
        board.Place(
            new(5, BoardEntity.Size.Y - 3),
            BlockEntity.CreateNormal(BlockColor.Red, new(2, 3))
        );

        var (_, sim) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );

        // 結果盤面で、全ブロック中最大の面積を持つものが、元々の 6 + 今回の 2 = 8 になっているか
        var maxArea = sim.Board.Select(p => p.Block.Size.X * p.Block.Size.Y).Max();
        maxArea.ShouldBe(8);
    }

    [Fact(DisplayName = "Test5: 他色の小さいブロックが複数隣接している時はブロック操作を選ぶべき")]
    public void Test5_PreferNormalBlockOverInefficientBomb()
    {
        var board = new BoardEntity();
        // 青の小さなブロック（1x1）をバラバラに置く
        board.Place(new(0, BoardEntity.Size.Y - 1), BlockEntity.CreateNormal(BlockColor.Blue));
        board.Place(new(5, BoardEntity.Size.Y - 1), BlockEntity.CreateNormal(BlockColor.Blue));

        var (blockEval, _) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );
        var (bombEval, _) = EvaluationService.UpdateDestination(
            SpawnBomb(board),
            board,
            _weights,
            null
        );

        // ボムの消去範囲に乏しい場合、UseBombPenalty によりブロック操作のスコアが上回るはず
        blockEval.TotalScore.ShouldBeGreaterThan(bombEval.TotalScore);
    }

    [Fact(DisplayName = "Test6: 他色の大きな塊があるときブロックよりボムを選ぶべき")]
    public void Test6_PreferBombAgainstLargeEnemyBlocks()
    {
        var board = new BoardEntity();
        // 中央に青の巨大な塊 (2x5 = 10マス) を配置
        board.Place(
            new(3, BoardEntity.Size.Y - 6),
            BlockEntity.CreateNormal(BlockColor.Blue, new(2, 5))
        );

        var (blockEval, _) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );
        var (bombEval, _) = EvaluationService.UpdateDestination(
            SpawnBomb(board),
            board,
            _weights,
            null
        );

        // 巨大ブロック消去の加点 > ボム使用ペナルティ となり、ボムが選ばれるべき
        bombEval.TotalScore.ShouldBeGreaterThan(blockEval.TotalScore);
    }

    [Fact(DisplayName = "Test7: 同色の大きな塊がある時ボムより大きなブロック生成を優先するべき")]
    public void Test7_PreferGrowthOverDestructionForSameColor()
    {
        var board = new BoardEntity();
        // 赤の巨大な塊
        board.Place(
            new(3, BoardEntity.Size.Y - 6),
            BlockEntity.CreateNormal(BlockColor.Red, new(2, 5))
        );

        var (blockEval, _) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );
        var (bombEval, _) = EvaluationService.UpdateDestination(
            SpawnBomb(board),
            board,
            _weights,
            null
        );

        // 同色ならボムより合体の方が BlockSize 的に有利
        blockEval.TotalScore.ShouldBeGreaterThan(bombEval.TotalScore);
    }

    [Fact(
        DisplayName = "Test8: 小さなブロックのまとまりより大きなブロック1つの方がスコアが高いべき"
    )]
    public void Test8_LargeBlockShouldScoreHigherThanMultipleSmallBlocks()
    {
        // 小さなブロック 1x1 が 4個隣接
        var board1 = new BoardEntity();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                board1.Place(new(x, y), BlockEntity.CreateNormal(BlockColor.Red));
            }
        }

        // 2x2 の巨大ブロックが 1個
        var board2 = new BoardEntity();
        board2.Place(new(0, 0), BlockEntity.CreateNormal(BlockColor.Red, new(2, 2)));

        var res1 = EvaluationService.Evaluate(CreateSimResult(board1), _weights);
        var res2 = EvaluationService.Evaluate(CreateSimResult(board2), _weights);

        // 単純な隣接よりも合体後のブロックの方が評価が高いことを確認
        res2.TotalScore.ShouldBeGreaterThan(res1.TotalScore);
    }
}

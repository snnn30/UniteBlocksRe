using Godot;
using Shouldly;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.Evaluation;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;

namespace UniteBlocksRe.Test.Models;

public class EvaluationServiceTests
{
    private readonly EvaluationWeight _weights = new DefaultEvaluationWeight();

    #region Helpers
    private OperatingBlocksEntity SpawnRedPair(BoardEntity board)
    {
        // デフォルト位置でのスポーン
        OperatingBlocksEntity.TrySpawnDouble(
            BlockEntity.CreateNormal(BlockColor.Red),
            BlockEntity.CreateNormal(BlockColor.Red),
            board,
            out var entity
        );
        return entity!;
    }

    private OperatingBlocksEntity SpawnBomb(BoardEntity board)
    {
        OperatingBlocksEntity.TrySpawnSingle(BlockEntity.CreateBomb(), board, out var entity);
        return entity!;
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

    [Fact(DisplayName = "低く積める場合は低い位置への配置を優先するテスト")]
    public void LowerPlacement_ShouldBePreferred()
    {
        var board = new BoardEntity();

        board.Place(new(3, BoardEntity.Size.Y - 1), BlockEntity.CreateObstacle());
        board.Place(new(3, BoardEntity.Size.Y - 2), BlockEntity.CreateObstacle());

        var (_, sim) = EvaluationService.UpdateDestination(
            SpawnRedPair(board),
            board,
            _weights,
            null
        );

        sim.ParentDestination.Y.ShouldBe(BoardEntity.Size.Y - 1);
        sim.ChildDestination.Y.ShouldBe(BoardEntity.Size.Y - 1);
    }

    [Fact(DisplayName = "同色ブロックが存在する場合は隣接配置を優先するテスト")]
    public void SameColorAdjacency_ShouldBePreferred()
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

    [Fact(DisplayName = "ゲームオーバー可能な配置を回避するテスト")]
    public void GameOverPlacement_ShouldBeAvoided()
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

        eval.Scores[EvaluationCriterion.CantSpawnPenalty].ShouldBe(0);
    }

    [Fact(DisplayName = "より大きな合体を作れる配置を優先するテスト")]
    public void LargerMerge_ShouldBePreferred()
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

    [Fact(DisplayName = "爆発効率が低い場合はボムより通常ブロックを優先するテスト")]
    public void InefficientBomb_ShouldNotBePreferred()
    {
        var board = new BoardEntity();

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

        blockEval.TotalScore.ShouldBeGreaterThan(bombEval.TotalScore);
    }

    [Fact(DisplayName = "巨大な敵ブロックが存在する場合は通常ブロックよりボムを優先するテスト")]
    public void Bomb_ShouldBePreferredAgainstLargeEnemyBlocks()
    {
        var board = new BoardEntity();

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

        bombEval.TotalScore.ShouldBeGreaterThan(blockEval.TotalScore);
    }

    [Fact(DisplayName = "同色の巨大ブロックが存在する場合はボムより合体を優先するテスト")]
    public void SameColorGrowth_ShouldBePreferredOverBomb()
    {
        var board = new BoardEntity();

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

        blockEval.TotalScore.ShouldBeGreaterThan(bombEval.TotalScore);
    }

    [Fact(DisplayName = "小ブロック群より巨大ブロック単体の方が高スコアになるテスト")]
    public void LargeBlock_ShouldScoreHigherThanMultipleSmallBlocks()
    {
        // 小さなブロック 1x1 が 4個隣接
        var board1 = new BoardEntity();
        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                board1.Place(new(x, y), BlockEntity.CreateNormal(BlockColor.Red));
            }
        }

        // 2x2 の巨大ブロックが 1個
        var board2 = new BoardEntity();
        board2.Place(new(0, 0), BlockEntity.CreateNormal(BlockColor.Red, new(2, 2)));

        var res1 = EvaluationService.Evaluate(CreateSimResult(board1), _weights);
        var res2 = EvaluationService.Evaluate(CreateSimResult(board2), _weights);

        res2.TotalScore.ShouldBeGreaterThan(res1.TotalScore);
    }
}

using Godot;
using Shouldly;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.Services;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Models.ValueObjects.Simulation;

namespace UniteBlocksRe.Test.Models.Services;

public class EvaluationTest
{
    private readonly ITestOutputHelper _output;

    private readonly BoardEvaluationWeights _boardWeights;
    private readonly ExplodeEvaluationWeights _explodeWeights;
    private readonly ActionSelector _actionSelector;

    private (bool Sucess, OperatingBlocksEntity Entity) SpawnRedPair(BoardEntity board) =>
        OperatingBlocksEntity.TrySpawnDouble(
            new(BlockColor.Red),
            new(BlockColor.Red),
            BoardEntity.SpawnPosition,
            BoardEntity.SpawnPosition + Vector2I.Up,
            board
        );

    private (bool Sucess, OperatingBlocksEntity Entity) SpawnBomb(BoardEntity board) =>
        OperatingBlocksEntity.TrySpawnSingle(BlockEntity.Bomb, BoardEntity.SpawnPosition, board);

    public EvaluationTest(ITestOutputHelper output)
    {
        _output = output;

        _boardWeights = new BoardEvaluationWeights
        {
            BlockSizeWeight = 10f,
            SameColorAdjacentWeight = 10f,
            HeightPenalty = -2f,
            ObstaclePenalty = -20f,
            DifferentColorAdjacentPenalty = -4f,
        };
        _explodeWeights = new ExplodeEvaluationWeights { Weight = 12f };
        _actionSelector = new ActionSelector(_boardWeights, _explodeWeights);
    }

    [Fact(DisplayName = "低く積むことを優先するべき")]
    public void Test1()
    {
        // 中央が少し盛り上がっている盤面
        var board = new BoardEntity();
        board.TrySetBlock(new Vector2I(3, BoardEntity.Size.Y - 1), BlockEntity.Obstacle);
        board.TrySetBlock(new Vector2I(3, BoardEntity.Size.Y - 2), BlockEntity.Obstacle);

        var bestResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);

        // できるだけ低くなるように置かれているか
        var allBlocks = bestResult.SimulatedBoard.GetAllBlocks();
        foreach (var b in allBlocks)
        {
            var pos = bestResult.SimulatedBoard.TryGetOrigin(b).Position;
            if (b.Type == BlockType.Normal)
            {
                pos.Y.ShouldBeGreaterThanOrEqualTo(BoardEntity.Size.Y - 1);
            }
        }
    }

    [Fact(DisplayName = "すでに同色のブロックがあるとき、隣接を狙うべき")]
    public void Test2()
    {
        // 右下に赤いブロックが1つある
        var board = new BoardEntity();
        var targetPos = new Vector2I(BoardEntity.Size.X - 1, BoardEntity.Size.Y - 1);
        board.TrySetBlock(targetPos, new BlockEntity(BlockColor.Red));

        var bestResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);

        var blocks = bestResult.SimulatedBoard.GetAllBlocks().ToArray();
        foreach (var block in blocks)
        {
            var pos = bestResult.SimulatedBoard.TryGetOrigin(block).Position;
            pos.Y.ShouldBe(BoardEntity.Size.Y - 1);
        }

        var xCoordinates = blocks
            .Select(block => bestResult.SimulatedBoard.TryGetOrigin(block).Position.X)
            .OrderBy(x => x)
            .ToList();

        xCoordinates.Count.ShouldBe(3);
        xCoordinates[0].ShouldBe(BoardEntity.Size.X - 3);
        xCoordinates[1].ShouldBe(BoardEntity.Size.X - 2);
        xCoordinates[2].ShouldBe(BoardEntity.Size.X - 1);
    }

    [Fact(DisplayName = "ゲームオーバーを確実に回避するべき")]
    public void Test3()
    {
        // スポーン地点の高さより１マス下以下を埋める
        var board = new BoardEntity();
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = BoardEntity.SpawnPosition.Y + 1; y < BoardEntity.Size.Y; y++)
            {
                board.TrySetBlock(new Vector2I(x, y), BlockEntity.Obstacle);
            }
        }

        var bestResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);

        bestResult.EvaluationResult.CantSpawnPenaltyScore.ShouldBe(0, 0.001f);
    }

    [Fact(DisplayName = "複数の塊が存在する時、より面積が大きくなる合体を優先するべき")]
    public void Test4()
    {
        // サイズの異なる2つの赤い塊を用意する
        var board = new BoardEntity();

        var redA = new BlockEntity(BlockType.Normal, BlockColor.Red, new Vector2I(2, 2));
        board.TrySetBlock(new Vector2I(0, BoardEntity.Size.Y - 2), redA);

        var redB = new BlockEntity(BlockType.Normal, BlockColor.Red, new Vector2I(2, 3));
        board.TrySetBlock(new Vector2I(5, BoardEntity.Size.Y - 3), redB);

        var bestResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);

        var maxBlock = bestResult
            .SimulatedBoard.GetAllBlocks()
            .OrderByDescending(b => b.Size.GetArea())
            .First();

        maxBlock.Size.GetArea().ShouldBe(8);
    }

    [Fact(DisplayName = "他色の小さいブロックが複数隣接している時ブロック操作を選ぶべき")]
    public void Test5()
    {
        // 他色の小さなブロックが隣接している
        var board = new BoardEntity();
        for (var y = 5; y < BoardEntity.Size.Y; y++)
        {
            board.TrySetBlock(new Vector2I(3, y), new BlockEntity(BlockColor.Blue));
        }

        var bestBlockResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);
        var bestBombResult = _actionSelector.GetBestMoveBomb(SpawnBomb(board).Entity);

        var BombTotalScore =
            bestBombResult.BoardEvaluationResult.TotalScore
            + bestBombResult.ExplodeEvaluationResult.Score;

        _output.WriteLine(
            $"""
            ボムのスコア : {BombTotalScore}
            ブロックのスコア : {bestBlockResult.EvaluationResult.TotalScore}
            """
        );
        // ブロック操作を選んでほしい
        BombTotalScore.ShouldBeLessThan(bestBlockResult.EvaluationResult.TotalScore);
    }

    [Fact(DisplayName = "他色の大きな塊があるときブロックよりボムを選ぶべき")]
    public void Test6()
    {
        // 他色の大きな塊が一つ
        var board = new BoardEntity();
        board.TrySetBlock(
            new Vector2I(3, 0),
            new BlockEntity(BlockType.Normal, BlockColor.Blue, new(2, 5))
        );
        BoardFaller.Fall(board);

        var bestBlockResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);
        var bestBombResult = _actionSelector.GetBestMoveBomb(SpawnBomb(board).Entity);

        var BombTotalScore =
            bestBombResult.BoardEvaluationResult.TotalScore
            + bestBombResult.ExplodeEvaluationResult.Score;

        _output.WriteLine(
            $"""
            ボムのスコア : {BombTotalScore}
            ブロックのスコア : {bestBlockResult.EvaluationResult.TotalScore}
            """
        );

        // ボム操作を選んでほしい
        BombTotalScore.ShouldBeGreaterThan(bestBlockResult.EvaluationResult.TotalScore);
    }

    [Fact(DisplayName = "同色の大きな塊がある時ボムより大きなブロック生成を優先するべき")]
    public void Test7()
    {
        // 同色の大きな塊が一つ
        var board = new BoardEntity();
        board.TrySetBlock(
            new Vector2I(3, 0),
            new BlockEntity(BlockType.Normal, BlockColor.Red, new(2, 5))
        );
        BoardFaller.Fall(board);

        var bestBlockResult = _actionSelector.GetBestMoveBlock(SpawnRedPair(board).Entity);
        var bestBombResult = _actionSelector.GetBestMoveBomb(SpawnBomb(board).Entity);

        var BombTotalScore =
            bestBombResult.BoardEvaluationResult.TotalScore
            + bestBombResult.ExplodeEvaluationResult.Score;

        _output.WriteLine(
            $"""
            ボムのスコア : {BombTotalScore}
            ブロックのスコア : {bestBlockResult.EvaluationResult.TotalScore}
            """
        );

        // ブロック操作を選んでほしい
        BombTotalScore.ShouldBeLessThan(bestBlockResult.EvaluationResult.TotalScore);
    }

    [Fact(DisplayName = "小さなブロックのまとまりより大きなブロック1つの方がスコアが高いべき")]
    public void Test8()
    {
        // 小さなブロック群を用意する
        var board1 = new BoardEntity();
        board1.TrySetBlock(new(0, 0), new BlockEntity(BlockColor.Red));
        board1.TrySetBlock(new(1, 0), new BlockEntity(BlockColor.Red));
        board1.TrySetBlock(new(0, 1), new BlockEntity(BlockColor.Red));
        board1.TrySetBlock(new(1, 1), new BlockEntity(BlockColor.Red));
        var board1Result = Evaluation.BoardEvaluate(board1, _boardWeights);

        // 大きなブロックを用意する
        var board2 = new BoardEntity();
        board2.TrySetBlock(new(0, 0), new BlockEntity(BlockType.Normal, BlockColor.Red, new(2, 2)));
        var board2Result = Evaluation.BoardEvaluate(board2, _boardWeights);

        _output.WriteLine(
            $"""
            小さなブロック群のスコア
                {board1Result}
            大きなブロックのスコア
                {board2Result}
            """
        );

        board2Result.TotalScore.ShouldBeGreaterThan(board1Result.TotalScore);
    }
}

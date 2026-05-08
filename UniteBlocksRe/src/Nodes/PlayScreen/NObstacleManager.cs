using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models.BoardServices;
using UniteBlocksRe.Nodes;

namespace UniteBlocksRe.src.Nodes.PlayScreen;

public partial class NObstacleManager : Node
{
    private IPlayScreen _screen;

    private float _obstacleRate = 4.5f;
    private const float RateSubSpeed = 0.018f;

    public void Init(IPlayScreen screen)
    {
        _screen = screen;
    }

    public override void _Process(double delta)
    {
        _obstacleRate -= RateSubSpeed * (float)delta;
        _obstacleRate = Math.Max(0.1f, _obstacleRate);
    }

    public void OnExploded(ExplodeResult result, PlayerSide side)
    {
        var playerContext = _screen.GetContext(side);
        var oppositeContext = _screen.GetContext(side.Opposite());

        var score = 0;
        foreach (var step in result.Steps)
        {
            foreach (var block in step.ExplodedBlocks)
            {
                var area = block.Size.GetArea();
                score += area * area;
            }
        }

        var currentCount = (int)(score / _obstacleRate);
        var playerObstacles = playerContext.ObstacleCounter.ObstacleCount;

        if (playerObstacles > 0) // 相殺する
        {
            var offset = Math.Min(playerObstacles, currentCount); // 相殺量
            playerContext.ObstacleCounter.ObstacleCount -= offset;
            currentCount -= offset;
        }

        if (currentCount > 0) // 相手に追加
        {
            oppositeContext.ObstacleCounter.ObstacleCount += currentCount;
            oppositeContext.ObstacleCounter.TurnCount = 4;
        }
    }

    public async Task OnTurnStart(PlayerSide side)
    {
        var playerContext = _screen.GetContext(side);

        playerContext.ObstacleCounter.TurnCount -= 1;
        if (playerContext.ObstacleCounter.TurnCount != 0)
        {
            return;
        }
        var obstacleCount = playerContext.ObstacleCounter.ObstacleCount;
        if (obstacleCount == 0)
        {
            return;
        }

        var result = ObstaclePlaceService.Execute(playerContext.Board.Model, obstacleCount);
        playerContext.ObstacleCounter.ObstacleCount -= result.PlacedCount;
        await playerContext.Board.SpawnObstacles(result);
    }
}

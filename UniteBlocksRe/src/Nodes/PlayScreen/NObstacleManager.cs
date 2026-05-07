using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models.BoardServices;
using UniteBlocksRe.Nodes.PlayScreen;

namespace UniteBlocksRe.src.Nodes.PlayScreen;

public partial class NObstacleManager : Node
{
    private IPlayerContext _playerContext;

    private float _obstacleRate = 4.5f;
    private const float RateSubSpeed = 0.018f;

    public void Init(IPlayerContext context)
    {
        _playerContext = context;
    }

    public override void _Process(double delta)
    {
        _obstacleRate -= RateSubSpeed * (float)delta;
        _obstacleRate = Math.Max(0.1f, _obstacleRate);
    }

    public void OnExploded(ExplodeResult result)
    {
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
        var playerObstacles = _playerContext.ObstacleCounter.ObstacleCount;

        if (playerObstacles > 0) // 相殺する
        {
            var offset = Math.Min(playerObstacles, currentCount); // 相殺量
            _playerContext.ObstacleCounter.ObstacleCount -= offset;
            currentCount -= offset;
        }

        if (currentCount > 0) // 相手に追加
        {
            _playerContext.OpponentContext.ObstacleCounter.ObstacleCount += currentCount;
            _playerContext.OpponentContext.ObstacleCounter.TurnCount = 4;
        }
    }

    public async Task OnTurnStart()
    {
        _playerContext.ObstacleCounter.TurnCount -= 1;
        if (_playerContext.ObstacleCounter.TurnCount != 0)
        {
            return;
        }
        var obstacleCount = _playerContext.ObstacleCounter.ObstacleCount;
        if (obstacleCount == 0)
        {
            return;
        }

        var result = ObstaclePlaceService.Execute(_playerContext.Board.Model, obstacleCount);
        _playerContext.ObstacleCounter.ObstacleCount -= result.PlacedCount;
        await _playerContext.Board.SpawnObstacles(result);
    }
}

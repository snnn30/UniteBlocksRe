using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes;

public partial class NPlayerScene : Node2D
{
    private NOperationManager _operationManager;
    private NBoard _board;
    private NBlockQueue _queue;
    private NBombGauge _bombGauge;
    private IOperationInputSource _inputSource;

    public void InitEnemy()
    {
        _inputSource = new EnemyInputSource(_operationManager);
        _operationManager.Init(_board, _bombGauge, _inputSource);
    }

    public void InitPlayer()
    {
        _inputSource = new PlayerInputSource();
        _operationManager.Init(_board, _bombGauge, _inputSource);
    }

    public override void _Ready()
    {
        _operationManager = GetNode<NOperationManager>("%OperationManager");
        _board = GetNode<NBoard>("%Board");
        _queue = GetNode<NBlockQueue>("%Queue");
        _bombGauge = GetNode<NBombGauge>("%BombGauge");
    }

    public async Task StartGameLoop()
    {
        _bombGauge.IsAutoCharging = true;

        while (true)
        {
            await _board.SpawnObstacles();
            _inputSource.UpdateStrategy(_board.Model, _bombGauge, _queue.Model);
            var (parent, spawnResult) = await Spawn();
            if (!spawnResult.Sucess)
            {
                Log.Info("Game Over");
                return;
            }
            await spawnResult.Task;

            await _operationManager.StartRun();

            _bombGauge.IsAutoCharging = false;
            await _board.Fall();
            await _board.Unite();
            if (parent.Type == BlockType.Bomb)
            {
                await _board.Explode(parent);
                await _board.Fall();
                await _board.Unite();
            }
            _bombGauge.IsAutoCharging = true;
        }
    }

    private async Task<(BlockEntity Parent, OperationResult SpawnResult)> Spawn()
    {
        if (_bombGauge.IsBombActive)
        {
            _bombGauge.TryUseBomb();
            var parent = BlockEntity.Bomb;
            var spawnResult = _operationManager.Spawn(parent);
            return (parent, spawnResult);
        }
        else
        {
            var (pair, _) = _queue.Dequeue();
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
            var parent = pair.Parent;
            var spawnResult = _operationManager.Spawn(pair.Parent, pair.Child);
            return (parent, spawnResult);
        }
    }
}

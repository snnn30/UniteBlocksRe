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

    public override void _Ready()
    {
        _operationManager = GetNode<NOperationManager>("%OperationManager");
        _board = GetNode<NBoard>("%Board");
        _queue = GetNode<NBlockQueue>("%Queue");
        _bombGauge = GetNode<NBombGauge>("%BombGauge");

        // var inputSource = new PlayerInputSource();
        var inputSource = new EnemyInputSource(_operationManager);

        _operationManager.Init(_board, _bombGauge, inputSource);
    }

    public async Task StartGameLoop()
    {
        _bombGauge.IsAutoCharging = true;

        while (true)
        {
            await _board.SpawnObstacles();

            OperationResult spawnResult;
            BlockEntity parent;
            if (_bombGauge.IsBombActive)
            {
                _bombGauge.TryUseBomb();
                parent = BlockEntity.Bomb;
                spawnResult = _operationManager.Spawn(parent);
            }
            else
            {
                var (pair, _) = _queue.Dequeue();
                await Task.Delay(TimeSpan.FromSeconds(0.2f));
                parent = pair.Parent;
                spawnResult = _operationManager.Spawn(pair.Parent, pair.Child);
            }

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
}

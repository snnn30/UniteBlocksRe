using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Nodes.OperationItem;

namespace UniteBlocksRe.Nodes;

public partial class NPlayerScene : Node2D
{
    private NOperationManager _operationManager;
    private NBoard _board;
    private NBlockQueue _queue;
    private NBombManager _bombManager;

    public override void _Ready()
    {
        _operationManager = GetNode<NOperationManager>("%OperationManager");
        _board = GetNode<NBoard>("%Board");
        _queue = GetNode<NBlockQueue>("%Queue");
        _bombManager = GetNode<NBombManager>("%BombManager");

        _operationManager.Init(_board);
    }

    public async Task StartGameLoop()
    {
        _bombManager.IsAutoCharging = true;
        _bombManager.InputActive = true;

        while (true)
        {
            await _board.SpawnObstacles();

            OperationResult spawnResult;
            BlockEntity parent;
            if (_bombManager.IsBombActive)
            {
                _bombManager.TryUseBomb();
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

            _bombManager.IsAutoCharging = false;
            await _board.Fall();
            await _board.Unite();

            if (parent.Type == BlockType.Bomb)
            {
                await _board.Explode(parent);
                await _board.Fall();
                await _board.Unite();
            }

            _bombManager.IsAutoCharging = true;
        }
    }
}

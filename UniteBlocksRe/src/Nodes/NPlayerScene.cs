using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;

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

        _operationManager.Init(_board);
    }

    public async Task StartGameLoop()
    {
        _bombGauge.IsAutoCharging = true;
        while (true)
        {
            var (pair, _) = _queue.Dequeue();

            await Task.Delay(TimeSpan.FromSeconds(0.2f));

            var spawnResult = _operationManager.Spawn(pair.Parent, pair.Child);
            if (!spawnResult.Sucess)
            {
                Log.Info("Game Over");
                return;
            }
            await spawnResult.Task;

            await _operationManager.StartRun();

            await _board.Fall();
            await _board.Unite();
        }
    }
}

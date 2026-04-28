using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Nodes.PlayerScene;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes;

public partial class NPlayerScene : Node2D, IPlayerContext
{
    public NOperationManager OperationManager { get; private set; }
    public NBoard Board { get; private set; }
    public NBlockQueue Queue { get; private set; }
    public NBombGauge BombGauge { get; private set; }
    public NObstacleCounter ObstacleCounter { get; private set; }
    public IOperationInputSource InputSource { get; private set; }
    public IPlayerContext OpponentContext { get; private set; }

    public void Init(IOperationInputSource inputSource, IPlayerContext opponentContext)
    {
        InputSource = inputSource;
        OpponentContext = opponentContext;

        OperationManager.Init(this);
        Board.Init(this);
    }

    public override void _Ready()
    {
        OperationManager = GetNode<NOperationManager>("%OperationManager");
        Board = GetNode<NBoard>("%Board");
        Queue = GetNode<NBlockQueue>("%Queue");
        BombGauge = GetNode<NBombGauge>("%BombGauge");
        ObstacleCounter = GetNode<NObstacleCounter>("%ObstacleCounter");
    }

    public async Task StartGameLoop()
    {
        BombGauge.IsAutoCharging = true;

        while (true)
        {
            await Board.SpawnObstacles();
            InputSource.UpdateStrategy(Board.Model, BombGauge, Queue.Model);
            var (parent, spawnResult) = await Spawn();
            if (!spawnResult.Sucess)
            {
                BombGauge.IsAutoCharging = false;
                Log.Info("Game Over");
                return;
            }
            await spawnResult.Task;

            await OperationManager.StartRun();

            BombGauge.IsAutoCharging = false;
            await Board.Fall();
            await Board.Unite();
            if (parent.Type == BlockType.Bomb)
            {
                await Board.Explode(parent);
                await Board.Fall();
                await Board.Unite();
            }
            BombGauge.IsAutoCharging = true;
        }
    }

    private async Task<(BlockEntity Parent, OperationResult SpawnResult)> Spawn()
    {
        if (BombGauge.IsBombActive)
        {
            BombGauge.TryUseBomb();
            var parent = BlockEntity.Bomb;
            var spawnResult = OperationManager.Spawn(parent);
            return (parent, spawnResult);
        }
        else
        {
            var (pair, _) = Queue.Dequeue();
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
            var parent = pair.Parent;
            var spawnResult = OperationManager.Spawn(pair.Parent, pair.Child);
            return (parent, spawnResult);
        }
    }
}

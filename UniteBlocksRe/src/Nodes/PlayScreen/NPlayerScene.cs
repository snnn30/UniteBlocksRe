using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Nodes.PlayScreen.Operation;
using UniteBlocksRe.src.Nodes.PlayScreen;

namespace UniteBlocksRe.Nodes.PlayScreen;

public partial class NPlayerScene : Node2D, IPlayerContext
{
    public NOperationManager OperationManager { get; private set; }
    public NBoard Board { get; private set; }
    public NBlockQueue Queue { get; private set; }
    public NBombGauge BombGauge { get; private set; }
    public NObstacleManager ObstacleManager { get; private set; }
    public NObstacleCounter ObstacleCounter { get; private set; }
    public IOperationInputSource InputSource { get; private set; }
    public IPlayerContext OpponentContext { get; private set; }

    public void Init(IOperationInputSource inputSource, IPlayerContext opponentContext)
    {
        InputSource = inputSource;
        OpponentContext = opponentContext;

        OperationManager.Init(this);
        Board.Init(this);
        ObstacleManager.Init(this);
    }

    public override void _Ready()
    {
        OperationManager = GetNode<NOperationManager>("%OperationManager");
        Board = GetNode<NBoard>("%Board");
        Queue = GetNode<NBlockQueue>("%Queue");
        BombGauge = GetNode<NBombGauge>("%BombGauge");
        ObstacleCounter = GetNode<NObstacleCounter>("%ObstacleCounter");
        ObstacleManager = GetNode<NObstacleManager>("%ObstacleManager");
    }

    public async Task StartGameLoop()
    {
        BombGauge.IsAutoCharging = true;

        while (true)
        {
            await ObstacleManager.OnTurnStart();

            if (!CheckCanSpawn())
            {
                return;
            }

            await OperationManager.Spawn();
            await OperationManager.StartRun();

            BombGauge.IsAutoCharging = false;
            await Board.ProcessChainReaction();
            BombGauge.IsAutoCharging = true;
        }
    }

    private bool CheckCanSpawn()
    {
        return Board.Model.CanPlace(BoardEntity.SpawnPosition, Vector2I.One);
    }
}

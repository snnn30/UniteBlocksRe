using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models;
using UniteBlocksRe.Nodes.PlayScreen.Operation;

namespace UniteBlocksRe.Nodes.PlayScreen;

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

            if (!CheckCanSpawn())
            {
                Log.Debug("GameOver");
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

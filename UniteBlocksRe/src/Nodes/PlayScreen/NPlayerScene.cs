using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Nodes.PlayScreen.PlayerScene;
using UniteBlocksRe.Nodes.PlayScreen.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes.PlayScreen;

public interface IPlayerContext
{
    PlayerSide PlayerSide { get; }
    NOperationManager OperationManager { get; }
    NBoard Board { get; }
    NBlockQueue Queue { get; }
    NBombGauge BombGauge { get; }
    NObstacleCounter ObstacleCounter { get; }
    IOperationInputSource InputSource { get; }
}

public partial class NPlayerScene : Node2D, IPlayerContext
{
    public PlayerSide PlayerSide { get; private set; }
    public NOperationManager OperationManager { get; private set; } = null!;
    public NBoard Board { get; private set; } = null!;
    public NBlockQueue Queue { get; private set; } = null!;
    public NBombGauge BombGauge { get; private set; } = null!;
    public NObstacleCounter ObstacleCounter { get; private set; } = null!;
    public IOperationInputSource InputSource { get; private set; } = null!;

    private IPlayScreen _playScreen = null!;

    public void Init(IOperationInputSource inputSource, PlayerSide side, IPlayScreen playScreen)
    {
        PlayerSide = side;
        InputSource = inputSource;
        _playScreen = playScreen;

        Board.Init(playScreen, side);
        OperationManager.Init(playScreen, side);
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
            await _playScreen.ObstacleManager.OnTurnStart(PlayerSide);

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

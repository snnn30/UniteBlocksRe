using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;
using UniteBlocksRe.Nodes.PlayScreen;
using UniteBlocksRe.Nodes.PlayScreen.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes;

public interface IPlayScreen
{
    NObstacleManager ObstacleManager { get; }
    IPlayerContext GetContext(PlayerSide side);
}

public partial class NPlayScreen : Control, IPlayScreen
{
    public IPlayerContext GetContext(PlayerSide side)
    {
        return side switch
        {
            PlayerSide.Left => _playerScene,
            PlayerSide.Right => _enemyScene,
            _ => throw new NotImplementedException(),
        };
    }

    private NPlayerScene _playerScene;
    private NPlayerScene _enemyScene;
    private NGameOverMessage _gameOverMessage;
    public NObstacleManager ObstacleManager { get; private set; }

    public override async void _Ready()
    {
        SetProcessInput(false);
        Initialize();
        var playerTask = _playerScene.StartGameLoop();
        var enemyTask = _enemyScene.StartGameLoop();
        var finishedTask = await Task.WhenAny(playerTask, enemyTask);

        var playerWin = finishedTask == enemyTask;
        _ = GameOver(playerWin);
    }

    private void Initialize()
    {
        _playerScene = GetNode<NPlayerScene>("%PlayerScene");
        _enemyScene = GetNode<NPlayerScene>("%EnemyScene");
        _gameOverMessage = GetNode<NGameOverMessage>("%GameOverMessage");
        ObstacleManager = GetNode<NObstacleManager>("%ObstacleManager");

        _gameOverMessage.Visible = false;
        ObstacleManager.Init(this);

        _playerScene.Init(new PlayerInputSource(), PlayerSide.Left, this);

        /*
        // 調整時にNPC同士を対戦させる
        _playerScene.Init(
            new EnemyInputSource(_playerScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            PlayerSide.Left,
            this
        );
        */

        _enemyScene.Init(
            new EnemyInputSource(_enemyScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            PlayerSide.Right,
            this
        );
    }

    private async Task GameOver(bool playerWin)
    {
        GetTree().Paused = true;
        if (playerWin)
        {
            _gameOverMessage.SetWinMessege();
        }
        else
        {
            _gameOverMessage.SetLoseMessage();
        }
        _gameOverMessage.Visible = true;
        await Task.Delay(TimeSpan.FromSeconds(1f));
        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("rotate_left"))
        {
            GetTree().Paused = false;
            var playScreen = GD.Load<PackedScene>("res://scenes/screens/play_screen.tscn");
            NGame.Instance.LoadScreen(playScreen);
        }
        if (@event.IsActionPressed("rotate_right"))
        {
            GetTree().Paused = false;
            var mainMenuScreen = GD.Load<PackedScene>("res://scenes/screens/main_menu_screen.tscn");
            NGame.Instance.LoadScreen(mainMenuScreen);
        }
    }
}

using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;
using UniteBlocksRe.Nodes.PlayerScene.Operation;
using UniteBlocksRe.Nodes.PlayScreen;
using UniteBlocksRe.src.Nodes.PlayScreen;

namespace UniteBlocksRe.Nodes;

public enum PlayerSide
{
    Left,
    Right,
}

public static class PlayerSideExtensions
{
    public static PlayerSide Opposite(this PlayerSide side)
    {
        return side == PlayerSide.Left ? PlayerSide.Right : PlayerSide.Left;
    }
}

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

    private bool _gameOver;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Initialize();
        var playerTask = _playerScene.StartGameLoop();
        var enemyTask = _enemyScene.StartGameLoop();
        var finishedTask = await Task.WhenAny(playerTask, enemyTask);

        var playerWin = finishedTask == enemyTask;
        GameOver(playerWin);
    }

    private void Initialize()
    {
        _playerScene = GetNode<NPlayerScene>("%PlayerScene");
        _enemyScene = GetNode<NPlayerScene>("%EnemyScene");
        _gameOverMessage = GetNode<NGameOverMessage>("%GameOverMessage");
        ObstacleManager = GetNode<NObstacleManager>("%ObstacleManager");

        _gameOverMessage.Visible = false;
        ObstacleManager.Init(this);

        // _playerScene.Init(new PlayerInputSource(), PlayerSide.Left, this);

        _playerScene.Init(
            new EnemyInputSource(_playerScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            PlayerSide.Left,
            this
        );

        _enemyScene.Init(
            new EnemyInputSource(_enemyScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            PlayerSide.Right,
            this
        );
    }

    private void GameOver(bool playerWin)
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
        _gameOver = true;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_gameOver)
        {
            return;
        }

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

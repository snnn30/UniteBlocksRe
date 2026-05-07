using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;
using UniteBlocksRe.Nodes.PlayerScene.Operation;
using UniteBlocksRe.Nodes.PlayScreen;

public partial class NpcTest : Node
{
    private NPlayerScene _playerScene;
    private NPlayerScene _enemyScene;

    public override async void _Ready()
    {
        _playerScene = GetNode<NPlayerScene>("%PlayerScene");
        _enemyScene = GetNode<NPlayerScene>("%EnemyScene");

        _playerScene.Init(
            new EnemyInputSource(_playerScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            _enemyScene
        );
        _enemyScene.Init(
            new EnemyInputSource(_enemyScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            _playerScene
        );

        var playerTask = _playerScene.StartGameLoop();
        var enemyTask = _enemyScene.StartGameLoop();
        var finishedTask = await Task.WhenAny(playerTask, enemyTask);

        GetTree().Paused = true;

        if (finishedTask == playerTask)
        {
            Log.Info("プレイヤーの敗北...");
        }
        else
        {
            Log.Info("プレイヤーの勝利!!!");
        }
    }
}

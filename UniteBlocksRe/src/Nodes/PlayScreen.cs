using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;
using UniteBlocksRe.Nodes.PlayerScene.Operation;
using UniteBlocksRe.Nodes.PlayScreen;
using UniteBlocksRe.Nodes.PlayScreen.Operation;

public partial class PlayScreen : Control
{
    private NPlayerScene _playerScene;
    private NPlayerScene _enemyScene;

    public override void _Ready()
    {
        _playerScene = GetNode<NPlayerScene>("%PlayerScene");
        _enemyScene = GetNode<NPlayerScene>("%EnemyScene");

        _playerScene.Init(new PlayerInputSource(), _enemyScene);
        _enemyScene.Init(
            new EnemyInputSource(_enemyScene, new NpcDecisionMaker(new DefaultEvaluationWeight())),
            _playerScene
        );

        _ = _playerScene.StartGameLoop();
        _ = _enemyScene.StartGameLoop();
    }
}

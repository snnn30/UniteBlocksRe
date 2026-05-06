using Godot;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.Evaluation.EvaluationWeights;
using UniteBlocksRe.Nodes.PlayerScene.Operation;
using UniteBlocksRe.Nodes.PlayScreen;

public partial class NpcTest : Node
{
    private NPlayerScene _scene;

    public override void _Ready()
    {
        _scene = GetNode<NPlayerScene>("%PlayerScene");

        var inputSource = new EnemyInputSource(
            _scene,
            new NpcDecisionMaker(new DefaultEvaluationWeight())
        );

        _scene.Init(inputSource, _scene);
        _ = _scene.StartGameLoop();
    }
}

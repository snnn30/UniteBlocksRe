using Godot;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes.Tests;

public partial class PlayerSceneTest : Node
{
    private NPlayerScene _scene;

    public override void _Ready()
    {
        _scene = GetNode<NPlayerScene>("%PlayerScene");
        // _scene.Init(new EnemyInputSource(_scene.OperationManager), _scene); // 本来は相手を渡すがここでは自身を渡す
        _scene.Init(new PlayerInputSource(), _scene);
        _ = _scene.StartGameLoop();
    }
}

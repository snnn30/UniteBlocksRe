using Godot;

namespace UniteBlocksRe.Nodes.Tests;

public partial class PlayerSceneTest : Node
{
    private NPlayerScene _scene;

    public override void _Ready()
    {
        _scene = GetNode<NPlayerScene>("%PlayerScene");

        _scene.InitEnemy();
        _ = _scene.StartGameLoop();
    }
}

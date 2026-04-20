using Godot;

namespace UniteBlocksRe.Nodes.Tests;

public partial class PlayerSceneTest : Node
{
    NPlayerScene _scene;

    public override void _Ready()
    {
        _scene = GetNode<NPlayerScene>("%PlayerScene");
        _ = _scene.StartGameLoop();
    }
}

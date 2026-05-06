using Godot;
using UniteBlocksRe.Nodes.PlayScreen;
using UniteBlocksRe.Nodes.PlayScreen.Operation;

namespace UniteBlocksRe.Nodes.Tests;

public partial class PlayerSceneTest : Node
{
    private NPlayerScene _scene;

    public override void _Ready()
    {
        _scene = GetNode<NPlayerScene>("%PlayerScene");
        _scene.Init(new PlayerInputSource(), _scene);
        _ = _scene.StartGameLoop();
    }
}

using Godot;
using NSubstitute;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Nodes.PlayerScene;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NOperationManagerTest : Node
{
    private NOperationManager _manager;

    public override async void _Ready()
    {
        _manager = GetNode<NOperationManager>("%Manager");

        var context = Substitute.For<IPlayerContext>();
        context.OperationManager.Returns(_manager);
        context.Board.Returns(GetNode<NBoard>("%Board"));
        context.InputSource.Returns(new PlayerInputSource());

        _manager.Init(context);

        Log.Info(
            """
            OperationManagerのテスト開始
            InputMapにしたがって横移動、回転、落下
            """
        );

        while (true)
        {
            await _manager
                .Spawn(new BlockEntity(BlockColor.Blue), new BlockEntity(BlockColor.Green))
                .Task;
            await _manager.StartRun();
        }
    }
}

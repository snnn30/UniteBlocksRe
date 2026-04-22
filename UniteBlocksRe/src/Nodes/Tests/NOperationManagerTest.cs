using Godot;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NOperationManagerTest : Node
{
    private NOperationManager _manager;
    private NBoard _board;

    public override async void _Ready()
    {
        _manager = GetNode<NOperationManager>("%Manager");
        _board = GetNode<NBoard>("%Board");

        _manager.Init(_board);

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

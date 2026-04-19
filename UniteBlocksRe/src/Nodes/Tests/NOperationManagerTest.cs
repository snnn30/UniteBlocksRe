using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NOperationManagerTest : Node
{
    private NOperationManager _manager;
    private NOperationItem _item;
    private NBoard _board;

    public override async void _Ready()
    {
        _manager = GetNode<NOperationManager>("%Manager");
        _item = GetNode<NOperationItem>("%Item");
        _board = GetNode<NBoard>("%Board");

        _item.Init(_board);
        _manager.Init(_item);

        Log.Info(
            """
            OperationManagerのテスト開始
            InputMapにしたがって横移動、回転、落下
            """
        );

        while (true)
        {
            await _manager.SpawnAndRun(
                new BlockEntity(BlockColor.Blue),
                new BlockEntity(BlockColor.Green)
            );
            await _item.Settle().Task;
        }
    }
}

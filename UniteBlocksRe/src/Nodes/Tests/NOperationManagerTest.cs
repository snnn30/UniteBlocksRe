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

    public override void _Ready()
    {
        _manager = GetNode<NOperationManager>("%Manager");
        _item = GetNode<NOperationItem>("%Item");
        _board = GetNode<NBoard>("%Board");

        _item.Init(_board);
        _manager.Init(_item);

        Log.Info(
            """
            OperationManagerのテスト開始
            1でスポーン, 2で設置、3でActivateInput切り替え、4でActivateAutoDrop切り替え
            InputMapにしたがって横移動、回転、落下
            """
        );
    }

    public override async void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                _ = _item.SpawnBlock(
                    new BlockEntity(BlockColor.Blue),
                    new BlockEntity(BlockColor.Green)
                );
                Log.Info("ブロック2つをスポーン");
            }
            if (key.Keycode == Key.Key2)
            {
                await _item.SetOnBoard();
                Log.Info("ボード上に設置");
            }
            if (key.Keycode == Key.Key3)
            {
                var target = !_manager.ActivateInput;
                _manager.ActivateInput = target;
                Log.Info($"ActivateInput切り替え {!target} > {target}");
            }
            if (key.Keycode == Key.Key4)
            {
                var target = !_manager.ActivateAutoDrop;
                _manager.ActivateAutoDrop = target;
                Log.Info($"ActivateAutoDrop切り替え {!target} > {target}");
            }
        }
    }
}

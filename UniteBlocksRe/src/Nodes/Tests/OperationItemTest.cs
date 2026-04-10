using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Nodes.Tests;

public partial class OperationItemTest : Node
{
    private NBoard _board;
    private NOperationItem _item;

    public override void _Ready()
    {
        _board = GetNode<NBoard>("%Board");
        _item = _board.OperationItem;

        Log.Info(
            $"""
            NOperationItemのテスト開始
            キー1で1つSpawn、キー2で2つSpawn、A,S,Dで移動、U,Iで回転、spaceで設置
            """
        );
    }

    public override async void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                _item.SpawnAndResetPos(new BlockEntity(BlockColor.Red));
                Log.Info($"ブロック1つをスポーン {_item.ParentPos}");
            }
            if (key.Keycode == Key.Key2)
            {
                _item.SpawnAndResetPos(
                    new BlockEntity(BlockColor.Blue),
                    new BlockEntity(BlockColor.Green)
                );
                Log.Info("ブロック2つをスポーン");
            }
            if (key.Keycode == Key.A)
            {
                (var sucess, var task) = _item.MoveLeft();
                Log.Info($"左移動 {(sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.D)
            {
                (var sucess, var task) = _item.MoveRight();
                Log.Info($"右移動 {(sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.S)
            {
                (var sucess, var task) = _item.Drop();
                Log.Info($"落下 {(sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.U)
            {
                (var sucess, var task) = _item.Rotate(false);
                Log.Info($"反時計周りの回転 {(sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.I)
            {
                (var sucess, var task) = _item.Rotate(true);
                Log.Info($"時計回りの回転 {(sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.Space)
            {
                Log.Info("ボード上に設置 Tween終了待ち");
                await _item.SetOnBoard();
                Log.Info("ボード上に設置 完了");
            }
        }
    }
}

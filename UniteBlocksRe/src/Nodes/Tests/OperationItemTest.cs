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
        _item = GetNode<NOperationItem>("%Item");

        _item.Init(_board);

        Log.Info(
            $"""
            NOperationItemのテスト開始
            キー1で1つSpawn、キー2で2つSpawn、A,S,Dで移動、U,Iで回転、spaceで設置
            """
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                var result = _item.Spawn(new BlockEntity(BlockColor.Red));
                Log.Info($"ブロック1つをスポーン  {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.Key2)
            {
                var result = _item.Spawn(
                    new BlockEntity(BlockColor.Blue),
                    new BlockEntity(BlockColor.Green)
                );
                Log.Info($"ブロック2つをスポーン  {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.A)
            {
                var result = _item.Move(false, 0.06f);
                Log.Info($"左移動 {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.D)
            {
                var result = _item.Move(true, 0.06f);
                Log.Info($"右移動 {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.S)
            {
                var result = _item.Drop(0.1f);
                Log.Info($"落下 {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.U)
            {
                var result = _item.Rotate(false, 0.2f);
                Log.Info($"反時計周りの回転 {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.I)
            {
                var result = _item.Rotate(true, 0.2f);
                Log.Info($"時計回りの回転 {(result.Sucess ? "成功" : "失敗")}");
            }
            if (key.Keycode == Key.Space)
            {
                var result = _item.Settle();
                Log.Info($"ボード上に設置  {(result.Sucess ? "成功" : "失敗")}");
            }
        }
    }
}

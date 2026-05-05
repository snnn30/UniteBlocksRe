using Godot;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models;
using UniteBlocksRe.src.Models.Block;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NBlockTest : Node
{
    private NBlock _block;

    public override void _Ready()
    {
        var block = BlockEntity.CreateNormal(BlockColor.Red);
        _block = NBlock.Create(block);
        AddChild(_block);
        _block.Position = new Vector2(100, 100);

        Log.Info(
            $"""
            NBlockのテスト開始
            初期モデルは {_block.Model}
            キー1～4でNormalType切り替え、AでBomb、SでObstacle、Dでアウトラインの切り替え
            キー5～9で各種アニメーション
            """
        );
    }

    public override async void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            var model = key.Keycode switch
            {
                Key.Key1 => BlockEntity.CreateNormal(BlockColor.Red, Vector2I.One),
                Key.Key2 => BlockEntity.CreateNormal(BlockColor.Green, new Vector2I(2, 2)),
                Key.Key3 => BlockEntity.CreateNormal(BlockColor.Blue, new Vector2I(6, 3)),
                Key.Key4 => BlockEntity.CreateNormal(BlockColor.Orange, new Vector2I(2, 8)),
                Key.A => BlockEntity.CreateObstacle(),
                Key.S => BlockEntity.CreateBomb(),
                _ => null,
            };
            if (model is not null)
            {
                _block.Model = model;
                Log.Info($"新しいモデルをセット {model}");
            }

            if (key.Keycode == Key.D)
            {
                _block.Outlined = !_block.Outlined;
                Log.Info($"アウトラインの切り替え {(_block.Outlined ? "オン" : "オフ")} ");
            }

            if (key.Keycode == Key.Key5)
            {
                Log.Info("ボード上に設置した時のアニメーション");
                await _block.PlayPlacedAnimeAsync();
                Log.Info("アニメーション完了");
            }
            if (key.Keycode == Key.Key6)
            {
                Log.Info("爆発アニメーション");
                await _block.PlayExplodeAnimeAsync();
                Log.Info("アニメーション完了");
            }
            if (key.Keycode == Key.Key7)
            {
                Log.Info("合体アニメーション");
                await _block.PlayPlacedAnimeAsync();
                Log.Info("アニメーション完了");
            }
            if (key.Keycode == Key.Key8)
            {
                Log.Info("落下時アニメーション");
                await _block.PlayFalledAnimeAsync();
                Log.Info("アニメーション完了");
            }
            if (key.Keycode == Key.Key9)
            {
                Log.Info("スポーンアニメーション");
                await _block.PlaySpawnAnimeAsync();
                Log.Info("アニメーション完了");
            }
        }
    }
}

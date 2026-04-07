using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NBlockTest : Node
{
    private NBlock _block;

    public override void _Ready()
    {
        var block = new BlockEntity(BlockType.Normal, BlockColor.Red, new Vector2I(1, 1));
        _block = NBlock.Create(block);
        AddChild(_block);
        _block.Position = new Vector2(100, 100);

        Log.Info(
            $"""
            NBlockのテスト開始
            初期モデルは {_block.Model}
            キー1～4でNormalType切り替え、キー5でBomb、キー6でObstacle、キー7でアウトラインの切り替え
            """
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            var model = key.Keycode switch
            {
                Key.Key1 => new BlockEntity(BlockType.Normal, BlockColor.Red, new Vector2I(1, 1)),
                Key.Key2 => new BlockEntity(BlockType.Normal, BlockColor.Green, new Vector2I(2, 2)),
                Key.Key3 => new BlockEntity(BlockType.Normal, BlockColor.Blue, new Vector2I(6, 3)),
                Key.Key4 => new BlockEntity(
                    BlockType.Normal,
                    BlockColor.Orange,
                    new Vector2I(2, 8)
                ),
                Key.Key5 => new BlockEntity(BlockType.Bomb, BlockColor.None, Vector2I.One),
                Key.Key6 => new BlockEntity(BlockType.Obstacle, BlockColor.None, Vector2I.One),
                _ => null,
            };
            if (model is not null)
            {
                _block.Model = model;
                Log.Info($"新しいモデルをセット {model}");
            }

            if (key.Keycode == Key.Key7)
            {
                _block.Outlined = !_block.Outlined;
                Log.Info($"アウトラインの切り替え {(_block.Outlined ? "オン" : "オフ")} ");
            }
        }
    }
}

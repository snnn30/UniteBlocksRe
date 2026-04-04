using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NBlockTest : Node
{
    private NBlock _block;

    public override void _Ready()
    {
        var block = new BlockEntity(BlockColor.Red, new Vector2I(1, 1));
        _block = NBlock.Create(block);
        AddChild(_block);
        _block.Position = new Vector2(100, 100);

        GD.Print($"初期モデル    {_block.Model}");
        GD.Print("キー1～4でモデルを切り替え\nキー5でアウトラインの切り替え");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            var model = key.Keycode switch
            {
                Key.Key1 => new BlockEntity(BlockColor.Red, new Vector2I(1, 1)),
                Key.Key2 => new BlockEntity(BlockColor.Green, new Vector2I(2, 2)),
                Key.Key3 => new BlockEntity(BlockColor.Blue, new Vector2I(6, 3)),
                Key.Key4 => new BlockEntity(BlockColor.Orange, new Vector2I(2, 8)),
                _ => null,
            };
            if (model is not null)
            {
                GD.Print($"新しいモデルをセット\n    {model}");
                _block.Model = model;
            }

            if (key.Keycode == Key.Key5)
            {
                GD.Print("アウトラインの切り替え");
                _block.Outlined = !_block.Outlined;
            }
        }
    }
}

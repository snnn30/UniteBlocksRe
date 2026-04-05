using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.ValueObjects;

namespace UniteBlocksRe.Nodes;

public partial class NBlock : Node2D
{
    public const int BaseSize = 40;

    private Control _visuals;
    private ColorRect _outline;
    private NinePatchRect _icon;

    private BlockEntity _model;
    public BlockEntity Model
    {
        get => _model;
        set
        {
            _model = value;
            Reload();
        }
    }

    public bool Outlined { get; set; } = false;

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _outline = GetNode<ColorRect>("%Outline");
        _icon = GetNode<NinePatchRect>("%Icon");
        Reload();
    }

    public override void _Process(double delta)
    {
        _outline.Visible = Outlined && NBeatManager.Instance.BeatCount % 2 == 0;
    }

    public static NBlock Create(BlockEntity model)
    {
        var nBlock = ResourceLoader
            .Load<PackedScene>("res://scenes/block.tscn", null, ResourceLoader.CacheMode.Reuse)
            .Instantiate<NBlock>(PackedScene.GenEditState.Disabled);

        nBlock.Model = model;
        return nBlock;
    }

    private void Reload()
    {
        if (Model == null || _visuals == null)
        {
            return;
        }

        LoadTex();
        Resize();
    }

    private void LoadTex()
    {
        var path = Model.Color switch
        {
            BlockColor.Red => "res://images/blocks/RedBlock.png",
            BlockColor.Green => "res://images/blocks/GreenBlock.png",
            BlockColor.Blue => "res://images/blocks/BlueBlock.png",
            BlockColor.Orange => "res://images/blocks/OrangeBlock.png",
            _ => throw new System.NotImplementedException(),
        };

        _icon.Texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
    }

    private void Resize()
    {
        _visuals.Size = Model.Size * BaseSize + new Vector2I(20, 20);
    }
}

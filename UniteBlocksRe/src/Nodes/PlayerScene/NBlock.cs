using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects;

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

    private Vector2 _visualsOriginalScale;

    public bool Outlined { get; set; } = false;

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _outline = GetNode<ColorRect>("%Outline");
        _icon = GetNode<NinePatchRect>("%Icon");
        _visualsOriginalScale = _visuals.Scale;
        Reload();
    }

    public override void _Process(double delta)
    {
        _outline.Visible = Outlined && NBeatManager.Instance.BeatCount % 2 == 0;
    }

    public static NBlock Create(BlockEntity model)
    {
        var nBlock = ResourceLoader
            .Load<PackedScene>(
                "res://scenes/blocks/block.tscn",
                null,
                ResourceLoader.CacheMode.Reuse
            )
            .Instantiate<NBlock>(PackedScene.GenEditState.Disabled);

        nBlock.Model = model;
        return nBlock;
    }

    public Task PlayFalledAnimeAsync()
    {
        var tween = CreateTween();
        tween
            .TweenMethod(
                Callable.From<Vector2>(x => _visuals.Scale = x),
                _visualsOriginalScale * 0.6f,
                _visualsOriginalScale,
                0.6f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
        return tween.WaitForFinished();
    }

    public Task PlayUniteAnimeAsync() => PlayFalledAnimeAsync();

    public Task PlaySpawnAnimeAsync()
    {
        var tween = CreateTween();
        tween
            .TweenMethod(
                Callable.From<Vector2>(x => _visuals.Scale = x),
                _visualsOriginalScale * 0.8f,
                _visualsOriginalScale,
                0.2f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quart);
        return tween.WaitForFinished();
    }

    public Task PlayPlacedAnimeAsync()
    {
        var tween = CreateTween();
        tween
            .TweenMethod(
                Callable.From<Vector2>(x => _visuals.Scale = x),
                _visualsOriginalScale * 0.6f,
                _visualsOriginalScale,
                0.1f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
        return tween.WaitForFinished();
    }

    public Task PlayExplodeAnimeAsync()
    {
        const float ExplodeDuration = 0.5f;

        var targetScale = _visualsOriginalScale * 1.5f;

        var tween = CreateTween();
        tween
            .TweenMethod(
                Callable.From<Vector2>(x => _visuals.Scale = x),
                _visualsOriginalScale,
                targetScale,
                ExplodeDuration / 2f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween
            .TweenMethod(
                Callable.From<Vector2>(x => _visuals.Scale = x),
                targetScale,
                Vector2.Zero,
                ExplodeDuration / 2f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        return tween.WaitForFinished();
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
        var root = "res://images/blocks";
        var file = (Model.Type, Model.Color) switch
        {
            (BlockType.Bomb, _) => "BombBlock.png",
            (BlockType.Obstacle, _) => "ObstacleBlock.png",
            (_, BlockColor.Red) => "RedBlock.png",
            (_, BlockColor.Green) => "GreenBlock.png",
            (_, BlockColor.Blue) => "BlueBlock.png",
            (_, BlockColor.Orange) => "OrangeBlock.png",
            _ => throw new System.NotImplementedException(),
        };
        var fullPath = $"{root}/{file}";

        _icon.Texture = ResourceLoader.Load<Texture2D>(
            fullPath,
            null,
            ResourceLoader.CacheMode.Reuse
        );
    }

    private void Resize()
    {
        _visuals.Size = Model.Size * BaseSize + new Vector2I(20, 20);
        _visuals.PivotOffset = _visuals.Size / 2;
    }
}

using Godot;
using UniteBlocksRe.Nodes.CommonUi;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene;

public partial class NBombGauge : Node2D
{
    public bool IsAutoCharging { get; set; } = false;
    public int ChargeSpeed { get; set; } = 1000;
    public int Value
    {
        get => _gauge.Value;
        set => _gauge.Value = value;
    }
    public int ChargedSegments => _gauge.ChargedSegments;
    public int TotalSegments => _gauge.Segments;

    private bool _isBombActive = false;
    public bool IsBombActive
    {
        get => _isBombActive;
        private set
        {
            _isBombActive = value;
            _icon.Modulate = value ? new Color(1f, 1f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Elastic)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(
                _icon,
                "scale",
                value ? _defaultIconScale * 1.2f : _defaultIconScale,
                0.4f
            );
        }
    }

    private NDividedCircularGauge _gauge = null!;
    private Sprite2D _icon = null!;
    private Vector2 _defaultIconScale;

    public override void _Ready()
    {
        _gauge = GetNode<NDividedCircularGauge>("%Gauge");
        _icon = GetNode<Sprite2D>("%Icon");
        _defaultIconScale = _icon.Scale;

        Value = 0;
        IsBombActive = false;
    }

    public override void _Process(double delta)
    {
        if (IsAutoCharging)
        {
            Value += (int)(ChargeSpeed * delta);
        }
    }

    public bool TrySetBombActive(bool active)
    {
        if (!active)
        {
            IsBombActive = false;
            return true;
        }

        if (ChargedSegments > 0)
        {
            IsBombActive = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryUseBomb()
    {
        if (!IsBombActive)
        {
            return false;
        }
        else
        {
            IsBombActive = false;
            Value -= NDividedCircularGauge.UnitsPerSegment;
            return true;
        }
    }
}

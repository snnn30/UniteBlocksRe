using Godot;
using UniteBlocksRe.Nodes.CommonUi;

namespace UniteBlocksRe.Nodes;

public partial class NBombGauge : Node2D
{
    public bool IsAutoCharging { get; set; } = false;
    public int ChargeSpeed { get; set; } = 300;
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
            UpdateIconModulate();
        }
    }

    private NDividedCircularGauge _gauge;
    private Sprite2D _icon;

    public override void _Ready()
    {
        _gauge = GetNode<NDividedCircularGauge>("%Gauge");
        _icon = GetNode<Sprite2D>("%Icon");
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

    private void UpdateIconModulate()
    {
        _icon.Modulate = IsBombActive ? new Color(1f, 1f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
    }
}

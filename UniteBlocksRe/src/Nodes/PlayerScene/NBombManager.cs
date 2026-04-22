using Godot;

namespace UniteBlocksRe.Nodes;

public partial class NBombManager : Node2D
{
    NBombGauge _bombGauge;

    public bool IsAutoCharging
    {
        get => _bombGauge.IsAutoCharging;
        set => _bombGauge.IsAutoCharging = value;
    }

    public bool InputActive { get; set; } = false;

    public bool IsBombActive => _bombGauge.IsBombActive;

    public override void _Ready()
    {
        _bombGauge = GetNode<NBombGauge>("%BombGauge");
        _bombGauge.ChargeSpeed = 1000;
    }

    public override void _Process(double delta)
    {
        if (!InputActive)
        {
            return;
        }
        if (Input.IsActionJustPressed("switch"))
        {
            _bombGauge.TrySetBombActive(!_bombGauge.IsBombActive);
        }
    }

    public bool TryUseBomb() => _bombGauge.TryUseBomb();
}

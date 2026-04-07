using Godot;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NBombGaugeTest : Node
{
    private NBombGauge _gauge;

    public override void _Ready()
    {
        _gauge = GetNode<NBombGauge>("%BombGauge");
        _gauge.ChargeSpeed = 1000;

        Log.Info(
            $"""
            NBombGaugeのテスト開始
            キー1で自動チャージのオンオフ切り替え、キー2でボムのオンオフ切り替え、キー3でボムを使用
            """
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                _gauge.IsAutoCharging = !_gauge.IsAutoCharging;
                Log.Info($"自動チャージ切り替え {(_gauge.IsAutoCharging ? "オン" : "オフ")}");
            }
            if (key.Keycode == Key.Key2)
            {
                Log.Info(
                    $"ボムアクティブ切り替え {(!_gauge.IsBombActive ? "オン" : "オフ")} {(_gauge.TrySetBombActive(!_gauge.IsBombActive) ? "成功" : "失敗")}"
                );
            }
            if (key.Keycode == Key.Key3)
            {
                Log.Info($"{(_gauge.TryUseBomb() ? "ボム使用 成功" : "ボム使用 失敗")}");
            }
        }
    }
}

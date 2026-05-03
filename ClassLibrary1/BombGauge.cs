using System.Collections.Immutable;
using UniteBlocksRe.Domain.BombGauges;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain;

public record BombGauge : Entity
{
    public static int UnitsPerSegment { get; } = 1000;

    public ImmutableArray<Gauge> Gauges { get; init; }
    public bool IsReady { get; init; }
    public bool IsAutoCharging { get; init; }
    public int ChargeSpeed { get; init; }

    public int ChargedSegments => Gauges.Count(g => g.IsFull);
    public int TotalSegments => Gauges.Length;
    public bool HasStock => ChargedSegments > 0;

    public BombGauge(
        int maxSegments,
        int initialValue = 0,
        bool isAutoCharging = false,
        int chargeSpeed = 1000
    )
    {
        var builders = ImmutableArray.CreateBuilder<Gauge>();
        var remainingValue = initialValue;

        for (var i = 0; i < maxSegments; i++)
        {
            var valueForThisSeg = Math.Min(remainingValue, UnitsPerSegment);
            builders.Add(new Gauge(valueForThisSeg, UnitsPerSegment));
            remainingValue -= valueForThisSeg;
        }

        Gauges = builders.ToImmutableArray();
        IsReady = false;
        IsAutoCharging = isAutoCharging;
        ChargeSpeed = chargeSpeed;
    }

    /// <summary>
    /// 時間の経過をシミュレートし、必要に応じてチャージを行う
    /// </summary>
    public BombGauge Tick(float deltaTime)
    {
        if (!IsAutoCharging || deltaTime <= 0)
        {
            return this;
        }
        else
        {
            return ApplyChargeAmount((int)(deltaTime * ChargeSpeed));
        }
    }

    /// <summary>
    /// 指定した量だけゲージを直接増加させる
    /// </summary>
    public BombGauge ApplyChargeAmount(int amount)
    {
        if (amount <= 0)
        {
            return this;
        }

        var nextGauges = Gauges.ToBuilder();
        var remaining = amount;

        for (var i = 0; i < nextGauges.Count; i++)
        {
            if (nextGauges[i].IsFull)
            {
                continue;
            }

            var space = nextGauges[i].Max - nextGauges[i].Value;
            var add = Math.Min(remaining, space);

            nextGauges[i] = nextGauges[i].Increase(add);
            remaining -= add;

            if (remaining <= 0)
            {
                break;
            }
        }

        return this with
        {
            Gauges = nextGauges.ToImmutable(),
        };
    }

    /// <summary>
    /// ボムを構える
    /// </summary>
    public (BombGauge NewState, bool Success) TryReady(bool ready)
    {
        if (!ready)
        {
            return (this with { IsReady = false }, true);
        }

        if (HasStock)
        {
            return (this with { IsReady = true }, true);
        }

        return (this, false);
    }

    /// <summary>
    /// ボムを1つ消費する
    /// </summary>
    public (BombGauge NewState, bool Success) TryConsume()
    {
        if (!IsReady || !HasStock)
        {
            return (this, false);
        }

        var nextGauges = Gauges.ToBuilder();

        // 充填されている最後のスロットから消費する
        var targetIndex = -1;
        for (var i = nextGauges.Count - 1; i >= 0; i--)
        {
            if (nextGauges[i].IsFull)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex != -1)
        {
            nextGauges[targetIndex] = new Gauge(0, UnitsPerSegment);
            return (this with { Gauges = nextGauges.ToImmutable(), IsReady = false }, true);
        }

        return (this, false);
    }
}

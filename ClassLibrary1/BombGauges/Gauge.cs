namespace UniteBlocksRe.Domain.BombGauges;

public record Gauge
{
    public int Value { get; init; }
    public int Max { get; init; }

    public float Ratio => Max > 0 ? (float)Value / Max : 0;
    public bool IsFull => Value >= Max;

    public Gauge(int value, int max)
    {
        Max = max;
        Value = Math.Clamp(value, 0, max);
    }

    public Gauge Increase(int amount) => new(Value + amount, Max);
}

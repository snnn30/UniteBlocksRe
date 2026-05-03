namespace UniteBlocksRe.Domain.Common;

public readonly record struct Vector2I(int X, int Y)
{
    public static readonly Vector2I Zero = new(0, 0);
    public static readonly Vector2I One = new(1, 1);
    public static readonly Vector2I Up = new(0, -1);
    public static readonly Vector2I Down = new(0, 1);
    public static readonly Vector2I Left = new(-1, 0);
    public static readonly Vector2I Right = new(1, 0);

    public static Vector2I operator +(Vector2I a, Vector2I b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2I operator -(Vector2I a, Vector2I b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2I operator *(Vector2I a, int scalar) => new(a.X * scalar, a.Y * scalar);

    public int Area => X * Y;

    public override string ToString() => $"({X}, {Y})";
}

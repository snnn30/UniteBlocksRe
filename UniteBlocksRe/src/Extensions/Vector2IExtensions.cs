using Godot;

namespace UniteBlocksRe.Extensions;

public static class Vector2IExtensions
{
    public static int GetArea(this Vector2I vector)
    {
        return vector.X * vector.Y;
    }
}

using System;
using Godot;

namespace UniteBlocksRe.Models.OperatingBlocks;

public enum MoveDirection
{
    Left,
    Right,
}

public static class MoveDirectionExtensions
{
    public static Vector2I ToVector2I(this MoveDirection direction)
    {
        return direction switch
        {
            MoveDirection.Left => Vector2I.Left,
            MoveDirection.Right => Vector2I.Right,
            _ => throw new ArgumentException("無効な値", nameof(direction)),
        };
    }
}

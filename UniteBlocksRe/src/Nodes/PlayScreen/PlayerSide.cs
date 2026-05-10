namespace UniteBlocksRe.Nodes.PlayScreen;

public enum PlayerSide
{
    Left,
    Right,
}

public static class PlayerSideExtensions
{
    public static PlayerSide Opposite(this PlayerSide side)
    {
        return side == PlayerSide.Left ? PlayerSide.Right : PlayerSide.Left;
    }
}

using Godot;

namespace UniteBlocksRe.src.Nodes.OperationItem;

public class RealPositions
{
    public Vector2 Parent { get; set; }
    public Vector2 Child { get; set; }

    public void Add(RealPositions othert)
    {
        Parent += othert.Parent;
        Child += othert.Child;
    }
}

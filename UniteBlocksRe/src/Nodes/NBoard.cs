using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes;

public partial class NBoard : Node2D
{
    private Control _visuals;

    public Vector2[,] BlockPositions { get; private set; } =
        new Vector2[BoardEntity.Size.X, BoardEntity.Size.Y];

    public Vector2 SpawnPosition { get; private set; }

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _visuals.Size = BoardEntity.Size * NBlock.BaseSize;
        _visuals.Position = -_visuals.Size / 2;

        var spawnIcon = GetNode<Sprite2D>("%SpawnIcon");

        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                BlockPositions[x, y] = new Vector2(x + 0.5f, y + 0.5f) * NBlock.BaseSize;
            }
        }

        spawnIcon.Position = BlockPositions[
            BoardEntity.SpawnPosition.X,
            BoardEntity.SpawnPosition.Y
        ];
        SpawnPosition = spawnIcon.Position + new Vector2(0, -NBlock.BaseSize);
    }
}

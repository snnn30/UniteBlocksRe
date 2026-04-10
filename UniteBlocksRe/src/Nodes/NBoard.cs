using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes;

public partial class NBoard : Node2D
{
    private Control _visuals;
    private Control _clipMask;

    public NOperationItem OperationItem { get; private set; }

    public Vector2[,] BlockPositions { get; private set; } =
        new Vector2[BoardEntity.Size.X, BoardEntity.Size.Y];

    public Vector2 SpawnPosition { get; private set; }

    public BoardEntity Model { get; private set; } = new BoardEntity();

    private NBlock[,] _blocks = new NBlock[BoardEntity.Size.X, BoardEntity.Size.Y];

    private readonly Dictionary<NBlock, Vector2I> _blockToPos = [];

    public override void _EnterTree()
    {
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                BlockPositions[x, y] = new Vector2(x + 0.5f, y + 0.5f) * NBlock.BaseSize;
            }
        }

        SpawnPosition = BlockPositions[BoardEntity.SpawnPosition.X, BoardEntity.SpawnPosition.Y];
    }

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _visuals.Size = BoardEntity.Size * NBlock.BaseSize;
        _visuals.Position = -_visuals.Size / 2;

        _clipMask = GetNode<Control>("%ClipMask");

        var spawnIcon = GetNode<Sprite2D>("%SpawnIcon");
        spawnIcon.Position = SpawnPosition;

        OperationItem = GetNode<NOperationItem>("%OperationItem");
        OperationItem.Init(this);
    }

    public bool TryReparentBlock(Vector2I gridPos, NBlock block)
    {
        _blocks[gridPos.X, gridPos.Y] = block;
        _blockToPos[block] = gridPos;
        block.Reparent(this);

        return Model.TrySetBlock(gridPos, block.Model);
    }
}

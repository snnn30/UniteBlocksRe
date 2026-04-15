using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Helpers;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes;

public partial class NBoard : Node2D
{
    public BoardEntity Model { get; private set; } = new BoardEntity();

    private Control _visuals;
    private Control _clipMask;

    private BiMap<NBlock, Vector2I> _blocks = [];

    public static readonly Lazy<Dictionary<Vector2I, Vector2>> s_realPositions = new(() =>
    {
        var size = BoardEntity.Size;
        var dic = new Dictionary<Vector2I, Vector2>();
        for (var x = 0; x < size.X; x++)
        {
            for (var y = 0; y < size.Y; y++)
            {
                dic[new Vector2I(x, y)] = new Vector2(x + 0.5f, y + 0.5f) * NBlock.BaseSize;
            }
        }
        return dic;
    });

    static NBoard()
    {
        for (var x = 0; x < BoardEntity.Size.X; x++)
        {
            for (var y = 0; y < BoardEntity.Size.Y; y++)
            {
                s_realPositions.Value[new Vector2I(x, y)] =
                    new Vector2(x + 0.5f, y + 0.5f) * NBlock.BaseSize;
            }
        }
    }

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _visuals.Size = BoardEntity.Size * NBlock.BaseSize;
        _visuals.Position = -_visuals.Size / 2;

        _clipMask = GetNode<Control>("%ClipMask");

        var spawnIcon = GetNode<Sprite2D>("%SpawnIcon");
        spawnIcon.Position = s_realPositions.Value[BoardEntity.SpawnPosition];
    }

    public void BringToFront(NBlock block)
    {
        _clipMask.MoveChild(block, -1);
    }

    public (NBlock block, Task task) SpawnBlock(BlockEntity entity, Vector2I pos)
    {
        var block = NBlock.Create(entity);
        _clipMask.AddChild(block);
        block.Position = s_realPositions.Value[pos];
        var task = block.PlaySpawnAnimeAsync();
        return (block, task);
    }

    public Task SetOnBoard(NBlock block, Vector2I pos)
    {
        if (Model.TrySetBlock(pos, block.Model) is false)
        {
            Log.Warn($"pos {pos} には置けない");
            return Task.CompletedTask;
        }

        _blocks.Add(block, pos);
        block.Position = s_realPositions.Value[pos];
        return block.PlayPlacedAnimeAsync();
    }
}

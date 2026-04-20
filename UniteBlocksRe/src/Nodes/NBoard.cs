using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Helpers;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Models.Services;

namespace UniteBlocksRe.Nodes;

public partial class NBoard : Node2D
{
    public BoardEntity Model { get; private set; } = new BoardEntity();

    private Control _visuals;
    private Control _clipMask;

    private readonly BiMap<NBlock, Vector2I> _blockLocations = [];
    private readonly Dictionary<BlockEntity, NBlock> _blockIdentities = [];

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

        Add(block, pos);
        return block.PlayPlacedAnimeAsync();
    }

    private void Add(NBlock block, Vector2I pos)
    {
        _blockIdentities.Add(block.Model, block);
        _blockLocations.Add(block, pos);
        block.Position = s_realPositions.Value[pos];
    }

    private void Remove(NBlock block)
    {
        _blockIdentities.Remove(block.Model);
        _blockLocations.RemoveByKey(block);
        block.QueueFree();
    }

    public async Task Fall()
    {
        var result = BoardFaller.Fall(Model);
        var targets = new HashSet<NBlock>();

        if (!result.HasChanged)
        {
            return;
        }

        var tween = CreateTween()
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out)
            .SetParallel(true);

        foreach (var step in result.Steps)
        {
            var block = _blockIdentities[step.Block];
            var from = s_realPositions.Value[step.From];
            var to = s_realPositions.Value[step.To];
            _blockLocations.ForceAdd(block, step.To);
            tween.TweenProperty(block, "position", to, 0.4f).From(from);
            targets.Add(block);
        }

        await tween.WaitForFinished();
        var tasks = new List<Task>();
        foreach (var block in targets)
        {
            tasks.Add(block.PlayFalledAnimeAsync());
        }
        await Task.WhenAll(tasks);
    }

    public Task Unite()
    {
        var result = BoardUniter.Unite(Model);
        List<Task> tasks = [];

        foreach (var step in result.Steps)
        {
            foreach (var block in step.RemovedBlocks)
            {
                var node = _blockIdentities[block];
                Remove(node);
            }
            var (created, _) = SpawnBlock(step.CreatedBlock, step.Position);
            Add(created, step.Position);
            tasks.Add(created.PlayUniteAnimeAsync());
        }
        return Task.WhenAll(tasks);
    }

    public Task Explode(BlockEntity bomb)
    {
        var result = BoardExploder.Explode(Model, bomb);

        async Task PlayAnimation()
        {
            foreach (var step in result.Steps)
            {
                var tasks = new List<Task>();
                foreach (var block in step.Exploded)
                {
                    var node = _blockIdentities[block];
                    BringToFront(node);
                    tasks.Add(node.PlayExplodeAnimeAsync());
                }
                await Task.WhenAll(tasks);
                foreach (var block in step.Exploded)
                {
                    Remove(_blockIdentities[block]);
                }
            }
        }

        return PlayAnimation();
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Helpers;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.Services;
using UniteBlocksRe.src.Nodes.PlayerScene;

namespace UniteBlocksRe.Nodes;

public partial class NBoard : Node2D
{
    public BoardEntity Model { get; private set; } = new BoardEntity();

    private Control _visuals;
    private Control _clipMask;
    private NObstacleCounter _playerObstacleCounter;
    private NObstacleCounter _opponentObstacleCounter;

    private readonly BiMap<NBlock, Vector2I> _blockLocations = [];
    private readonly Dictionary<BlockEntity, NBlock> _blockIdentities = [];

    public static Vector2 GetRealPosition(Vector2I gridPos) =>
        new Vector2(gridPos.X + 0.5f, gridPos.Y + 0.5f) * NBlock.BaseSize;

    public void Init(IPlayerContext context)
    {
        _playerObstacleCounter = context.ObstacleCounter;
        _opponentObstacleCounter = context.OpponentContext.ObstacleCounter;
    }

    public override void _Ready()
    {
        _visuals = GetNode<Control>("%Visuals");
        _visuals.Size = BoardEntity.Size * NBlock.BaseSize;
        _visuals.Position = -_visuals.Size / 2;

        _clipMask = GetNode<Control>("%ClipMask");

        var spawnIcon = GetNode<Sprite2D>("%SpawnIcon");
        spawnIcon.Position = GetRealPosition(BoardEntity.SpawnPosition);
    }

    #region ブロック管理

    /// <summary>
    /// モデルからノードを作成、紐づけてボード上に登録する
    /// すでにModel側でボード上に設置されていることが前提
    /// </summary>
    private NBlock RegisterBlock(
        BlockEntity entity,
        Vector2I gridPos,
        Vector2? initialWorldPos = null
    )
    {
        var nBlock = NBlock.Create(entity);
        _clipMask.AddChild(nBlock);
        nBlock.Position = initialWorldPos ?? GetRealPosition(gridPos);

        _blockIdentities[entity] = nBlock;
        _blockLocations.ForceAdd(nBlock, gridPos);

        return nBlock;
    }

    /// <summary>
    /// ボードからノードと管理情報を削除する
    /// Model側の削除は行わない
    /// </summary>
    private void UnregisterBlock(BlockEntity entity)
    {
        if (_blockIdentities.Remove(entity, out var nBlock))
        {
            _blockLocations.RemoveByKey(nBlock);
            nBlock.QueueFree();
        }
    }

    #endregion

    #region 公開メソッド

    public void AddAsBoardElement(Node node)
    {
        if (node == null)
        {
            return;
        }
        _clipMask.AddChild(node);
    }

    public void BringToFront(Node node)
    {
        if (node == null || node.GetParent() != _clipMask)
        {
            return;
        }
        _clipMask.MoveChild(node, -1);
    }

    public async Task SetOnBoardAsync(NBlock block, Vector2I pos)
    {
        if (!Model.TrySetBlock(pos, block.Model))
        {
            Log.Warn($"pos {pos} には置けない");
            return;
        }

        if (block.GetParent() == null)
        {
            _clipMask.AddChild(block);
        }
        else if (block.GetParent() != _clipMask)
        {
            block.GetParent().RemoveChild(block);
            _clipMask.AddChild(block);
        }
        _blockIdentities[block.Model] = block;
        _blockLocations.Add(block, pos);
        block.Position = GetRealPosition(pos);

        await block.PlayPlacedAnimeAsync();
    }

    public async Task Fall()
    {
        var result = BoardFaller.Fall(Model);
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
            var nBlock = _blockIdentities[step.Block];
            _blockLocations.ForceAdd(nBlock, step.To);

            tween
                .TweenProperty(nBlock, "position", GetRealPosition(step.To), 0.4f)
                .From(GetRealPosition(step.From));
        }

        await tween.WaitForFinished();
        await Task.WhenAll(
            result.Steps.Select(s => _blockIdentities[s.Block].PlayFalledAnimeAsync())
        );
    }

    public Task Unite()
    {
        var result = BoardUniter.Unite(Model);
        List<Task> tasks = [];

        foreach (var step in result.Steps)
        {
            foreach (var block in step.RemovedBlocks)
            {
                UnregisterBlock(block);
            }
            var nBlock = RegisterBlock(step.CreatedBlock, step.Position);
            tasks.Add(nBlock.PlayUniteAnimeAsync());
        }
        return Task.WhenAll(tasks);
    }

    public async Task Explode(BlockEntity bomb)
    {
        var result = BoardExploder.Explode(Model, bomb);

        foreach (var step in result.Steps)
        {
            var tasks = step.Exploded.Select(b =>
            {
                var node = _blockIdentities[b];
                BringToFront(node);
                return node.PlayExplodeAnimeAsync();
            });

            _opponentObstacleCounter.AddCount(step);
            await Task.WhenAll(tasks);
            foreach (var block in step.Exploded)
            {
                UnregisterBlock(block);
            }
        }
        _opponentObstacleCounter.OnEndExplode();
    }

    public async Task SpawnObstacles()
    {
        var count = _playerObstacleCounter.ViewCount;
        var result = BoardObstaclePlacer.Place(Model, count, 5);
        if (!result.Placed)
        {
            return;
        }

        _playerObstacleCounter.SubCount(result);

        var tween = CreateTween()
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out)
            .SetParallel(true);

        foreach (var colmun in result.Colmuns.Values)
        {
            var lowest = colmun.Blocks.Max(b => b.position.Y);

            foreach (var (entity, pos) in colmun.Blocks)
            {
                var targetPos = GetRealPosition(pos);
                var offset = new Vector2(0, -(lowest + 2) * NBlock.BaseSize);
                var startPos = targetPos + offset;

                var nBlock = RegisterBlock(entity, pos, startPos);

                tween.TweenProperty(nBlock, "position", targetPos, 1.6f).From(startPos);
            }
        }

        await tween.WaitForFinished();
    }

    #endregion
}

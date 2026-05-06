using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Nodes.PlayScreen;

public partial class NBoard : Node2D
{
    public BoardEntity Model { get; private set; } = new BoardEntity();

    private Control _visuals;
    private Control _clipMask;
    private NObstacleCounter _playerObstacleCounter;
    private NObstacleCounter _opponentObstacleCounter;

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

    #region 公開メソッド

    public void AddBlockAsChild(NBlock node)
    {
        if (node == null)
        {
            return;
        }

        if (node.GetParent() is { } parentNode)
        {
            parentNode.RemoveChild(node);
        }
        _clipMask.AddChild(node);
    }

    public void BringToFront(NBlock node)
    {
        if (node == null || node.GetParent() != _clipMask)
        {
            return;
        }
        _clipMask.MoveChild(node, -1);
    }

    public async Task SetOnBoardAsync(NBlock block, Vector2I pos)
    {
        Model.Place(pos, block.Model);
        _blockIdentities[block.Model] = block;
        block.Position = GetRealPosition(pos);
        await block.PlayPlacedAnimeAsync();
    }

    public async Task<ProcessResult> ProcessChainReaction()
    {
        var processResult = BoardService.Process(Model);

        foreach (var step in processResult.Steps)
        {
            if (step is FallResult fallResult)
            {
                await Fall(fallResult);
            }
            else if (step is UniteResult uniteResult)
            {
                await Unite(uniteResult);
            }
            else if (step is ExplodeResult explodeResult)
            {
                await Explode(explodeResult);
            }
            else
            {
                Log.Error($"受け取り予定のない行動 {step}");
            }
        }

        return processResult;
    }

    public async Task SpawnObstacles()
    {
        var count = _playerObstacleCounter.ViewCount;
        var result = BoardService.ObstaclePlace(Model, count);
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

                var nBlock = NBlock.Create(entity);
                AddBlockAsChild(nBlock);
                _blockIdentities.Add(entity, nBlock);
                nBlock.Position = startPos;

                tween.TweenProperty(nBlock, "position", targetPos, 1.6f).From(startPos);
            }
        }

        await tween.WaitForFinished();
    }

    #endregion

    #region 非公開メソッド
    private async Task Fall(FallResult result)
    {
        var tween = CreateTween()
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out)
            .SetParallel(true);

        foreach (var step in result.Movements)
        {
            var node = _blockIdentities[step.Block];

            tween
                .TweenProperty(node, "position", GetRealPosition(step.To), 0.4f)
                .From(GetRealPosition(step.From));
        }

        await tween.WaitForFinished();
        await Task.WhenAll(
            result.Movements.Select(s => _blockIdentities[s.Block].PlayFalledAnimeAsync())
        );
    }

    private Task Unite(UniteResult result)
    {
        List<Task> tasks = [];

        foreach (var step in result.Steps)
        {
            foreach (var block in step.RemovedBlocks)
            {
                var node = _blockIdentities[block];
                _blockIdentities.Remove(block);
                node.QueueFree();
            }
            var newNode = NBlock.Create(step.CreatedBlock);
            AddBlockAsChild(newNode);
            _blockIdentities.Add(step.CreatedBlock, newNode);
            newNode.Position = GetRealPosition(Model.GetPositionOf(step.CreatedBlock));

            tasks.Add(newNode.PlayUniteAnimeAsync());
        }
        return Task.WhenAll(tasks);
    }

    private async Task Explode(ExplodeResult result)
    {
        Log.Debug("Explode Anime Start");
        foreach (var step in result.Steps)
        {
            var tasks = step.ExplodedBlocks.Select(b =>
            {
                var node = _blockIdentities[b];
                BringToFront(node);
                return node.PlayExplodeAnimeAsync();
            });

            _opponentObstacleCounter.AddCount(step);
            await Task.WhenAll(tasks);
            foreach (var block in step.ExplodedBlocks)
            {
                var node = _blockIdentities[block];
                _blockIdentities.Remove(block);
                node.QueueFree();
            }
        }
        _opponentObstacleCounter.OnEndExplode();
    }

    #endregion
}

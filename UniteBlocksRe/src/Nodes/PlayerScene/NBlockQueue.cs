using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.Nodes;

public partial class NBlockQueue : Node2D
{
    private record TargetTransform(Vector2 Position, Vector2 Scale);

    private record NBlockPair(Node2D Root, NBlock Parent, NBlock Child);

    private TargetTransform _spawnPoint;
    private TargetTransform _nextnextPoint;
    private TargetTransform _nextPoint;
    private TargetTransform _exitPoint;

    private NBlockPair _nextPair;
    private NBlockPair _nextNextPair;
    private NBlockPair _spawnPair;
    private readonly BlockQueueEntity _blockQueueEntity = new();

    private Tween _activeTween;

    public override void _Ready()
    {
        var spawnNode = GetNode<Node2D>("%Spawn");
        var nextnextNode = GetNode<Node2D>("%NextNext");
        var nextNode = GetNode<Node2D>("%Next");
        var exitPointNode = GetNode<Node2D>("%ExitPoint");

        _spawnPoint = new(spawnNode.Position, spawnNode.Scale);
        _nextnextPoint = new(nextnextNode.Position, nextnextNode.Scale);
        _nextPoint = new(nextNode.Position, nextNode.Scale);
        _exitPoint = new(exitPointNode.Position, exitPointNode.Scale);

        {
            var parent = nextNode.GetNode<NBlock>("Parent");
            var child = nextNode.GetNode<NBlock>("Child");
            parent.Model = _blockQueueEntity.Next.Parent;
            child.Model = _blockQueueEntity.Next.Child;
            _nextPair = new(nextNode, parent, child);
        }
        {
            var parent = nextnextNode.GetNode<NBlock>("Parent");
            var child = nextnextNode.GetNode<NBlock>("Child");
            parent.Model = _blockQueueEntity.NextNext.Parent;
            child.Model = _blockQueueEntity.NextNext.Child;
            _nextNextPair = new(nextnextNode, parent, child);
        }
        {
            var parent = spawnNode.GetNode<NBlock>("Parent");
            var child = spawnNode.GetNode<NBlock>("Child");
            _spawnPair = new(spawnNode, parent, child);
        }
    }

    public ((BlockEntity Parent, BlockEntity Child) Entities, Task AnimationTask) Dequeue()
    {
        _activeTween?.FastForwardToCompletion();

        var tween = CreateTween()
            .SetParallel(true)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);
        _activeTween = tween;

        var output = _blockQueueEntity.Dequeue();
        _spawnPair.Parent.Model = _blockQueueEntity.NextNext.Parent;
        _spawnPair.Child.Model = _blockQueueEntity.NextNext.Child;

        MoveBlockPair(tween, _spawnPair, _nextnextPoint);
        MoveBlockPair(tween, _nextNextPair, _nextPoint);
        MoveBlockPair(tween, _nextPair, _exitPoint);

        async Task WaitAndFinalize()
        {
            await tween.WaitForFinished();

            _nextPair.Root.Position = _spawnPoint.Position;
            _nextPair.Root.Scale = _spawnPoint.Scale;
            (_nextPair, _nextNextPair, _spawnPair) = (_nextNextPair, _spawnPair, _nextPair);

            _activeTween = null;
        }

        return (output, WaitAndFinalize());
    }

    private static void MoveBlockPair(Tween tween, NBlockPair pair, TargetTransform target)
    {
        const float Duration = 0.5f;
        tween.TweenProperty(pair.Root, "position", target.Position, Duration);
        tween.TweenProperty(pair.Root, "scale", target.Scale, Duration);
    }
}

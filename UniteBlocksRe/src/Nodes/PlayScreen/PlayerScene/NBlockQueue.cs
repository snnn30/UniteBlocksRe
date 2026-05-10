using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene;

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
    public BlockQueueEntity Model { get; } = new();

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
            parent.Model = Model.Next.Parent;
            child.Model = Model.Next.Child;
            _nextPair = new(nextNode, parent, child);
        }
        {
            var parent = nextnextNode.GetNode<NBlock>("Parent");
            var child = nextnextNode.GetNode<NBlock>("Child");
            parent.Model = Model.NextNext.Parent;
            child.Model = Model.NextNext.Child;
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

        var output = Model.Dequeue();
        _spawnPair.Parent.Model = Model.NextNext.Parent;
        _spawnPair.Child.Model = Model.NextNext.Child;

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

using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Nodes.PlayScreen;

namespace UniteBlocksRe.Nodes.Tests;

public partial class NBlockQueueTest : Node
{
    private NBlockQueue _queue;

    public override void _Ready()
    {
        _queue = GetNode<NBlockQueue>("BlockQueue");
        Log.Info(
            $"""
            NBlockQueueのテスト開始
            キー1でDeque
            """
        );
    }

    public override async void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                var result = _queue.Dequeue();
                Log.Info(
                    $"Deque Parent:{result.Entities.Parent.Color} Child:{result.Entities.Child.Color}"
                );
                await result.AnimationTask;
                Log.Info("アニメーション完了");
            }
        }
    }
}

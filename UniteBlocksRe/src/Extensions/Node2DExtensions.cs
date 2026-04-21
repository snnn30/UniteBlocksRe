using System.Collections.Generic;
using Godot;

namespace UniteBlocksRe.Extensions;

public static partial class Node2DExtensions
{
    public class OffsetHandler
    {
        private readonly Node2D _node;
        public Vector2 Val { get; set; }

        public OffsetHandler(Node2D node, Vector2 val)
        {
            _node = node;
            Val = val;
        }

        public void Apply() => _node.ApplyOffset(this);
    }

    private static readonly Dictionary<
        Node2D,
        (Vector2 BasePosition, List<OffsetHandler> Offsets, OffsetWatcher watcher)
    > s_data = [];

    public static bool IsOperatingWithOffset(this Node2D node)
    {
        return s_data.ContainsKey(node);
    }

    public static OffsetHandler AddOffset(this Node2D node)
    {
        List<OffsetHandler> list;
        if (s_data.ContainsKey(node))
        {
            list = s_data[node].Offsets;
        }
        else
        {
            var basePosition = node.Position;
            list = [];

            var watcher = new OffsetWatcher(node);
            node.AddChild(watcher);
            node.TreeExiting += () => s_data.Remove(node);

            s_data.Add(node, (basePosition, list, watcher));
        }

        var handler = new OffsetHandler(node, Vector2.Zero);
        list.Add(handler);
        return handler;
    }

    private static void ApplyOffset(this Node2D node, OffsetHandler handler)
    {
        if (!s_data.ContainsKey(node))
        {
            return;
        }
        var (_, offsets, watcher) = s_data[node];
        if (!offsets.Contains(handler))
        {
            return;
        }

        offsets.Remove(handler);
        var newTuple = s_data[node];
        newTuple.BasePosition += handler.Val;
        s_data[node] = newTuple;

        if (offsets.Count == 0)
        {
            node.Position = newTuple.BasePosition;
            s_data.Remove(node);
            watcher.QueueFree();
        }
    }

    private partial class OffsetWatcher(Node2D target) : Node
    {
        public override void _Process(double delta)
        {
            if (!s_data.ContainsKey(target))
            {
                return;
            }
            var (basePosition, offsets, _) = s_data[target];
            var totalOffset = Vector2.Zero;
            foreach (var offset in offsets)
            {
                totalOffset += offset.Val;
            }

            target.Position = basePosition + totalOffset;
        }
    }
}

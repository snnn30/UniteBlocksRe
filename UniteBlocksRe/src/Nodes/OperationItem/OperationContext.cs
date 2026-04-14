using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes.OperationItem;

public class OperationContext
{
    public NBlock Parent { get; set; }
    public NBlock Child { get; set; }
    public NBoard Board { get; init; }
    public Node OperationItem { get; init; }
    public OperationState OperationState { get; set; }
    public bool IsLocked { get; set; } = false;

    private readonly List<Task> _activeAnims = [];

    public OperationContext(NBoard board, Node operationItem, OperationState state)
    {
        Board = board;
        OperationItem = operationItem;
        OperationState = state;
    }

    public Task WaitForAnimations()
    {
        return Task.WhenAll(_activeAnims);
    }

    public Func<Task> TrackAnim(Func<Task> func)
    {
        return () =>
        {
            var task = func?.Invoke() ?? Task.CompletedTask;
            _activeAnims.Add(task);
            _ = task.ContinueWith(_ =>
            {
                _activeAnims.Remove(task);
            });
            return task;
        };
    }

    public bool CanOperate(OperationPhase requiredPhase, OperationState before)
    {
        if (IsLocked)
        {
            Log.Debug("ロックされている");
            return false;
        }
        else if (OperationState.Phase != requiredPhase)
        {
            Log.Debug($"フェーズが{requiredPhase}ではなく{OperationState.Phase}になっている");
            return false;
        }
        else if (before != OperationState)
        {
            Log.Warn(
                $"""
                現時点のOperationStateと判断時点のものに差異がある
                判断時点 {before}
                現時点 {OperationState}
                """
            );
            return false;
        }
        else
        {
            return true;
        }
    }
}

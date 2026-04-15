using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes.OperationItem;

public class OperationContext
{
    public NBlock Parent { get; set; }
    public NBlock Child { get; set; }
    public Vector2I ParentPos { get; set; }
    public Vector2I ChildPos { get; set; }

    public bool IsLocked { get; set; }
    public bool IsBetweenCells { get; set; }
    public OperationPhase Phase { get; set; }

    public NBoard Board { get; init; }

    private readonly List<Task> _activeAnims = [];

    public OperationContext(NBoard board)
    {
        Board = board;
    }

    public Tween CreateTween()
    {
        return Board.CreateTween();
    }

    public OperationContext CreateSnapshot()
    {
        return (OperationContext)this.MemberwiseClone();
    }

    public Task WaitForAnimations()
    {
        return Task.WhenAll(_activeAnims);
    }

    public Task TrackAnim(Task anim)
    {
        var task = anim ?? Task.CompletedTask;
        _activeAnims.Add(task);
        _ = task.ContinueWith(_ =>
        {
            _activeAnims.Remove(task);
        });
        return task;
    }

    public bool CanOperate(OperationPhase requiredPhase)
    {
        if (IsLocked)
        {
            Log.Debug("ロックされている");
            return false;
        }
        else if (Phase != requiredPhase)
        {
            Log.Debug($"フェーズが{requiredPhase}ではなく{Phase}になっている");
            return false;
        }
        else
        {
            return true;
        }
    }
}

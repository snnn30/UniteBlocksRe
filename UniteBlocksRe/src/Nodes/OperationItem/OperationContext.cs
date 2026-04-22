using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Logging;

namespace UniteBlocksRe.src.Nodes.OperationItem;

public class OperationContext
{
    public NBlock Parent { get; set; }
    public NBlock Child { get; set; }
    public Vector2I ParentPos { get; set; }
    public Vector2I ChildPos { get; set; }
    public bool HasChild => Child != null;

    public bool IsLocked { get; set; }
    public bool IsBetweenCells { get; set; }
    public OperationPhase Phase { get; set; }

    public NBoard Board { get; init; }

    private readonly List<Task> _activeAnims = [];

    public OperationContext(NBoard board)
    {
        Board = board;
    }

    public OperationContext CreateSnapshot()
    {
        return (OperationContext)MemberwiseClone();
    }

    public Tween CreateTween()
    {
        return Board.CreateTween();
    }

    public Task WaitForAnimations()
    {
        return Task.WhenAll(_activeAnims);
    }

    public async Task TrackAnim(Task anim)
    {
        var task = anim ?? Task.CompletedTask;
        _activeAnims.Add(task);
        await task;
        _activeAnims.Remove(task);
    }

    public bool CanOperate(OperationPhase requiredPhase)
    {
        if (IsLocked)
        {
            Log.Debug("ロックされている", 1);
            return false;
        }
        else if (Phase != requiredPhase)
        {
            Log.Debug($"フェーズが{requiredPhase}ではなく{Phase}になっている", 1);
            return false;
        }
        else
        {
            return true;
        }
    }
}

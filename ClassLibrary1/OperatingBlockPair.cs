using UniteBlocksRe.Domain.BlockOperation;
using UniteBlocksRe.Domain.Common;

namespace UniteBlocksRe.Domain;

public record OperatingBlockPair : Entity
{
    public Block Parent { get; }
    public Block? Child { get; }
    public Vector2I ParentPos { get; }
    public Vector2I ChildPos { get; }

    public bool IsHalfUp { get; }

    #region Spawn

    public static bool TrySpawnSingle(Block parent, Board board, out OperatingBlockPair? state)
    {
        var sucess = board.CanPlace(board.SpawnPos, parent);
        if (!sucess)
        {
            state = null;
            return false;
        }
        else
        {
            state = new OperatingBlockPair(parent, board.SpawnPos, false);
            return true;
        }
    }

    public static bool TrySpawnDouble(
        Block parent,
        Block child,
        Board board,
        out OperatingBlockPair? state
    )
    {
        var parentPos = board.SpawnPos;
        var childPos = board.SpawnPos + Vector2I.Up;

        var sucess = board.CanPlace(parentPos, parent);
        sucess &= board.CanPlace(childPos, child);
        if (!sucess)
        {
            state = null;
            return false;
        }
        else
        {
            state = new OperatingBlockPair(parent, child, parentPos, childPos, false);
            return true;
        }
    }

    #endregion

    #region Private Constructer

    private OperatingBlockPair(
        Block parent,
        Block? child,
        Vector2I parentPos,
        Vector2I childPos,
        bool isHalfUp
    )
    {
        Parent = parent;
        Child = child;
        ParentPos = parentPos;
        ChildPos = childPos;
        IsHalfUp = isHalfUp;
    }

    private OperatingBlockPair(Block parent, Vector2I parentPos, bool isHalfUp)
    {
        Parent = parent;
        Child = null;
        ParentPos = parentPos;
        ChildPos = Vector2I.Zero;
        IsHalfUp = isHalfUp;
    }

    #endregion

    #region Operation

    public Board PlaceOnBoard(Board board)
    {
        var nextBoard = board.Place(ParentPos, Parent);
        if (Child != null)
        {
            nextBoard = board.Place(ChildPos, Child);
        }
        return nextBoard;
    }

    public bool TryDrop(Board board, out OperatingBlockPair nextState)
    {
        if (!CanDrop(board))
        {
            nextState = this;
            return false;
        }
        var (nextParentPos, nextChildPos) = CalcNextPosDrop();
        var nextIsHalfUp = !IsHalfUp;

        nextState = new(Parent, Child, nextParentPos, nextChildPos, nextIsHalfUp);
        return true;
    }

    public bool TryMove(MoveDirection direction, Board board, out OperatingBlockPair nextState)
    {
        if (!CanMove(direction, board))
        {
            nextState = this;
            return false;
        }
        var (nextParentPos, nextChildPos) = CalcNextPosMove(direction);
        nextState = new(Parent, Child, nextParentPos, nextChildPos, IsHalfUp);
        return true;
    }

    /// <summary>
    /// <code>
    /// 親基準のシフト回転
    /// □　　　　■□
    /// ■×　　　　×
    /// 子基準のシフト回転
    /// □
    /// ■×　　■□×
    /// </code>
    /// </summary>
    public (bool Sucess, bool PivotIsChild, bool IsShift) TryRotate(
        RotateDirection direction,
        Board board,
        out OperatingBlockPair nextState
    )
    {
        if (CanRotate(direction, pivotIsChild: false, isShift: false, board)) // 親基準の通常回転
        {
            var (nextParentPos, nextChildPos) = CalcNextPosRotate(direction, false, false);
            nextState = new(Parent, Child, nextParentPos, nextChildPos, IsHalfUp);
            return (true, false, false);
        }
        else if (CanRotate(direction, pivotIsChild: false, isShift: true, board)) // 親基準のシフト回転
        {
            var (nextParentPos, nextChildPos) = CalcNextPosRotate(direction, false, true);
            nextState = new(Parent, Child, nextParentPos, nextChildPos, IsHalfUp);
            return (true, false, true);
        }
        else if (CanRotate(direction, pivotIsChild: true, isShift: true, board)) // 子基準のシフト回転
        {
            var (nextParentPos, nextChildPos) = CalcNextPosRotate(direction, true, true);
            nextState = new(Parent, Child, nextParentPos, nextChildPos, IsHalfUp);
            return (true, true, true);
        }
        else if (CanRotate(direction, pivotIsChild: true, isShift: false, board)) // 子基準の通常回転
        {
            var (nextParentPos, nextChildPos) = CalcNextPosRotate(direction, true, false);
            nextState = new(Parent, Child, nextParentPos, nextChildPos, IsHalfUp);
            return (true, true, false);
        }
        else
        {
            nextState = this;
            return (false, false, false);
        }
    }

    #endregion

    #region Validation
    private bool CanDrop(Board board)
    {
        var (targetParentPos, targetChildPos) = CalcNextPosDrop();

        var canPlace = board.CanPlace(targetParentPos, Parent);
        if (Child != null)
        {
            canPlace &= board.CanPlace(targetChildPos, Child);
        }
        return canPlace;
    }

    private bool CanMove(MoveDirection direction, Board board)
    {
        var (targetParentPos, targetChildPos) = CalcNextPosMove(direction);
        var canPlace = board.CanPlace(targetParentPos, Parent);
        canPlace &= board.CanPlace(targetParentPos + Vector2I.Up, Parent) || !IsHalfUp;

        if (Child != null)
        {
            canPlace &= board.CanPlace(targetChildPos, Child);
            canPlace &= board.CanPlace(targetChildPos + Vector2I.Up, Child) || !IsHalfUp;
        }
        return canPlace;
    }

    private bool CanRotate(RotateDirection direction, bool pivotIsChild, bool isShift, Board board)
    {
        if (Child == null)
        {
            return false;
        }

        var (targetParentPos, targetChildPos) = CalcNextPosRotate(direction, pivotIsChild, isShift);

        var canPlace = board.CanPlace(targetParentPos, Parent);
        canPlace &= board.CanPlace(targetParentPos + Vector2I.Up, Parent) || !IsHalfUp;
        canPlace &= board.CanPlace(targetChildPos, Child);
        canPlace &= board.CanPlace(targetChildPos + Vector2I.Up, Child) || !IsHalfUp;
        return canPlace;
    }

    #endregion

    #region Calculate

    private (Vector2I TargetParentPos, Vector2I TargetChildPos) CalcNextPosDrop()
    {
        if (IsHalfUp)
        {
            return (ParentPos, ChildPos);
        }
        var targetParentPos = ParentPos + Vector2I.Down;
        var targetChildPos = ChildPos + Vector2I.Down;

        return (targetParentPos, targetChildPos);
    }

    private (Vector2I TargetParentPos, Vector2I TargetChildPos) CalcNextPosMove(
        MoveDirection direction
    )
    {
        var vec = direction.Offset;
        var targetParentPos = ParentPos + vec;
        var targetChildPos = ChildPos + vec;
        return (targetParentPos, targetChildPos);
    }

    private (Vector2I TargetParentPos, Vector2I TargetChildPos) CalcNextPosRotate(
        RotateDirection direction,
        bool pivotIsChild,
        bool isShift
    )
    {
        if (!isShift && !pivotIsChild)
        {
            return (ParentPos, CalcNextPosRotate(direction, ParentPos, ChildPos));
        }
        if (!isShift && pivotIsChild)
        {
            return (CalcNextPosRotate(direction, ChildPos, ParentPos), ChildPos);
        }

        var pivotPos = pivotIsChild ? ChildPos : ParentPos;
        var moverPos = pivotIsChild ? ParentPos : ChildPos;

        var nextMoverPos = CalcNextPosRotate(direction, pivotPos, moverPos);
        var relativeNextMoverPos = nextMoverPos - pivotPos; // Pivotから見た通常回転で置く位置

        var targetPivotPos = moverPos;
        var targetMoverPos = moverPos + relativeNextMoverPos;

        return pivotIsChild ? (targetMoverPos, targetPivotPos) : (targetPivotPos, targetMoverPos);
    }

    /// <summary>
    /// axiPosを基準にmoverPosを回転させる
    /// </summary>
    /// <returns>回転後のmoverPosの位置</returns>
    private static Vector2I CalcNextPosRotate(
        RotateDirection direction,
        Vector2I axisPos,
        Vector2I moverPos
    )
    {
        if (direction == RotateDirection.CW) { }
        var relativePos = moverPos - axisPos;
        var rotatedRelative = direction switch
        {
            var d when d == RotateDirection.CW => new Vector2I(-relativePos.Y, relativePos.X),
            var d when d == RotateDirection.ACW => new Vector2I(relativePos.Y, -relativePos.X),
            _ => throw new ArgumentException("無効な入力", nameof(direction)),
        };

        var targetMoverPos = axisPos + rotatedRelative;
        return targetMoverPos;
    }

    #endregion
}

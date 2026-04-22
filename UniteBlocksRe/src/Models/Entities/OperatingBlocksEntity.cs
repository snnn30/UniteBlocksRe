using System;
using Godot;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Models.Entities;

public class OperatingBlocksEntity
{
    public BlockEntity Parent { get; }
    public BlockEntity Child { get; }
    public Vector2I ParentPos { get; private set; }
    public Vector2I ChildPos { get; private set; }

    public bool HasChild => Child != null;
    public bool IsHalfUp { get; private set; } = false;

    private readonly BoardEntity _board;

    #region Spawn

    public static (bool Sucess, OperatingBlocksEntity Entity) TrySpawnSingle(
        BlockEntity parent,
        Vector2I parentPos,
        BoardEntity board
    )
    {
        var sucess = board.CanPlace(parentPos, parent);
        if (!sucess)
        {
            return (false, null);
        }
        else
        {
            var entity = new OperatingBlocksEntity(parent, parentPos, board);
            return (true, entity);
        }
    }

    public static (bool Sucess, OperatingBlocksEntity Entity) TrySpawnDouble(
        BlockEntity parent,
        BlockEntity child,
        Vector2I parentPos,
        Vector2I childPos,
        BoardEntity board
    )
    {
        var sucess = board.CanPlace(parentPos, parent);
        sucess &= board.CanPlace(childPos, child);
        if (!sucess)
        {
            return (false, null);
        }
        else
        {
            var entity = new OperatingBlocksEntity(parent, child, parentPos, childPos, board);
            return (true, entity);
        }
    }

    #endregion

    #region Private Constructer

    private OperatingBlocksEntity(
        BlockEntity parent,
        BlockEntity child,
        Vector2I parentPos,
        Vector2I childPos,
        BoardEntity board
    )
    {
        Parent = parent;
        Child = child;
        ParentPos = parentPos;
        ChildPos = childPos;
        _board = board;
    }

    private OperatingBlocksEntity(BlockEntity parent, Vector2I parentPos, BoardEntity board)
    {
        Parent = parent;
        Child = null;
        ParentPos = parentPos;
        ChildPos = Vector2I.Zero;
        _board = board;
    }

    #endregion

    #region Operation

    public bool TryDrop()
    {
        if (!CanDrop())
        {
            return false;
        }
        (ParentPos, ChildPos) = CalcNextPosDrop();
        IsHalfUp = !IsHalfUp;

        return true;
    }

    public bool TryMove(MoveDirection direction)
    {
        if (!CanMove(direction))
        {
            return false;
        }
        (ParentPos, ChildPos) = CalcNextPosMove(direction);
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
    public (bool Sucess, bool PivotIsChild, bool IsShift) TryRotate(RotationDirection direction)
    {
        if (CanRotate(direction, pivotIsChild: false, isShift: false)) // 親基準の通常回転
        {
            (ParentPos, ChildPos) = CalcNextPosRotate(direction, false, false);
            return (true, false, false);
        }
        else if (CanRotate(direction, pivotIsChild: false, isShift: true)) // 親基準のシフト回転
        {
            (ParentPos, ChildPos) = CalcNextPosRotate(direction, false, true);
            return (true, false, true);
        }
        else if (CanRotate(direction, pivotIsChild: true, isShift: true)) // 子基準のシフト回転
        {
            (ParentPos, ChildPos) = CalcNextPosRotate(direction, true, true);
            return (true, true, true);
        }
        else if (CanRotate(direction, pivotIsChild: true, isShift: false)) // 子基準の通常回転
        {
            (ParentPos, ChildPos) = CalcNextPosRotate(direction, true, false);
            return (true, true, false);
        }
        else
        {
            return (false, false, false);
        }
    }

    #endregion

    #region Validation
    private bool CanDrop()
    {
        var (targetParentPos, targetChildPos) = CalcNextPosDrop();

        var canPlace = _board.CanPlace(targetParentPos, Parent);
        if (HasChild)
        {
            canPlace &= _board.CanPlace(targetChildPos, Child);
        }
        return canPlace;
    }

    private bool CanMove(MoveDirection direction)
    {
        var (targetParentPos, targetChildPos) = CalcNextPosMove(direction);
        var canPlace = _board.CanPlace(targetParentPos, Parent);
        canPlace &= _board.CanPlace(targetParentPos + Vector2I.Up, Parent) || !IsHalfUp;

        if (HasChild)
        {
            canPlace &= _board.CanPlace(targetChildPos, Child);
            canPlace &= _board.CanPlace(targetChildPos + Vector2I.Up, Child) || !IsHalfUp;
        }
        return canPlace;
    }

    private bool CanRotate(RotationDirection direction, bool pivotIsChild, bool isShift)
    {
        if (!HasChild)
        {
            return false;
        }

        var (targetParentPos, targetChildPos) = CalcNextPosRotate(direction, pivotIsChild, isShift);

        var canPlace = _board.CanPlace(targetParentPos, Parent);
        canPlace &= _board.CanPlace(targetParentPos + Vector2I.Up, Parent) || !IsHalfUp;
        canPlace &= _board.CanPlace(targetChildPos, Child);
        canPlace &= _board.CanPlace(targetChildPos + Vector2I.Up, Child) || !IsHalfUp;
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
        var vec = direction.ToVector2I();
        var targetParentPos = ParentPos + vec;
        var targetChildPos = ChildPos + vec;
        return (targetParentPos, targetChildPos);
    }

    private (Vector2I TargetParentPos, Vector2I TargetChildPos) CalcNextPosRotate(
        RotationDirection direction,
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
        RotationDirection direction,
        Vector2I axisPos,
        Vector2I moverPos
    )
    {
        var relativePos = moverPos - axisPos;
        var rotatedRelative = direction switch
        {
            RotationDirection.CW => new Vector2I(-relativePos.Y, relativePos.X),
            RotationDirection.ACW => new Vector2I(relativePos.Y, -relativePos.X),
            _ => throw new ArgumentException("無効な入力", nameof(direction)),
        };

        var targetMoverPos = axisPos + rotatedRelative;
        return targetMoverPos;
    }

    #endregion
}

using System;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes;

public partial class NOperationItem : Node
{
    private OperatingBlocksEntity _entity;
    private NBoard _board;

    public NBlock Parent { get; private set; }
    public NBlock Child { get; private set; }
    public Vector2I ParentPos => _entity.ParentPos;
    public Vector2I ChildPos => _entity.ChildPos;

    #region Public Method

    public void Init(NBoard board)
    {
        _board = board;
    }

    public OperationResult Settle()
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Settle);
        }
        if (_entity.IsHalfUp)
        {
            Log.Warn("マスの半分の高さにある");
            return OperationResult.Failed(OperationType.Settle);
        }

        var task = new Func<NBlock, NBlock, Task>(
            async (parent, child) =>
            {
                var parentAnim = _board.SetOnBoardAsync(parent, _entity.ParentPos);
                var childAnim =
                    child != null
                        ? _board.SetOnBoardAsync(child, _entity.ChildPos)
                        : Task.CompletedTask;
                await Task.WhenAll(parentAnim, childAnim);
                parent.Outlined = false;
            }
        )(Parent, Child);

        Reset();
        return OperationResult.Succeeded(task, OperationType.Settle);
    }

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null)
    {
        if (child == null)
        {
            return SpawnSingle(parent);
        }
        else
        {
            return SpawnDouble(parent, child);
        }
    }

    public OperationResult Rotate(RotateDirection direction, float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Rotate);
        }

        var preParentPos = _entity.ParentPos;
        var preChildPos = _entity.ChildPos;

        var result = _entity.TryRotate(direction);

        if (result.Sucess && !result.IsShift)
        {
            var task = NormalRotateAnim(
                direction,
                duration,
                preParentPos,
                preChildPos,
                result.PivotIsChild
            );
            return OperationResult.Succeeded(task, OperationType.Rotate);
        }
        else if (result.Sucess && result.IsShift)
        {
            var task = ShiftRotateAnim(
                direction,
                duration,
                preParentPos,
                preChildPos,
                result.PivotIsChild
            );
            return OperationResult.Succeeded(task, OperationType.Rotate);
        }
        else
        {
            return OperationResult.Failed(OperationType.Rotate);
        }
    }

    public OperationResult Move(MoveDirection direction, float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Move);
        }

        var sucess = _entity.TryMove(direction);
        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Move);
        }

        var task = new Func<Task>(async () =>
        {
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            var parentHandler = Parent.AddOffset();
            var childHandler = Child?.AddOffset();

            tween.TweenMethod(
                Callable.From<Vector2>(v =>
                {
                    parentHandler.Val = v;
                    if (Child != null)
                    {
                        childHandler.Val = v;
                    }
                }),
                Vector2.Zero,
                (Vector2)direction.ToVector2I() * NBlock.BaseSize,
                duration
            );

            await tween.WaitForFinished();
            parentHandler.Apply();
            childHandler?.Apply();
        })();

        return OperationResult.Succeeded(task, OperationType.Move);
    }

    public OperationResult Drop(float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Drop);
        }

        var sucess = _entity.TryDrop();
        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Drop);
        }

        var task = new Func<Task>(async () =>
        {
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In);

            var parentHandler = Parent.AddOffset();
            var childHandler = Child?.AddOffset();
            tween.TweenMethod(
                Callable.From<Vector2>(v =>
                {
                    parentHandler.Val = v;
                    if (childHandler != null)
                    {
                        childHandler.Val = v;
                    }
                }),
                Vector2.Zero,
                Vector2.Down * NBlock.BaseSize * 0.5f,
                duration
            );

            await tween.WaitForFinished();
            parentHandler.Apply();
            childHandler?.Apply();
        })();

        return OperationResult.Succeeded(task, OperationType.Drop);
    }

    #endregion

    #region Private Method

    private OperationResult SpawnDouble(BlockEntity parent, BlockEntity child)
    {
        Reset();

        var parentPos = BoardEntity.SpawnPosition;
        var childPos = parentPos + Vector2I.Up;

        var (sucess, entity) = OperatingBlocksEntity.TrySpawnDouble(
            parent,
            child,
            parentPos,
            childPos,
            _board.Model
        );

        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Spawn);
        }
        _entity = entity;

        Parent = NBlock.Create(parent);
        _board.AddAsBoardElement(Parent);
        Parent.Position = NBoard.GetRealPosition(parentPos);

        Child = NBlock.Create(child);
        _board.AddAsBoardElement(Child);
        Child.Position = NBoard.GetRealPosition(childPos);

        Parent.Outlined = true;
        _board.BringToFront(Parent);

        var anim = Task.WhenAll(Parent.PlaySpawnAnimeAsync(), Child.PlaySpawnAnimeAsync());
        return OperationResult.Succeeded(anim, OperationType.Spawn);
    }

    private OperationResult SpawnSingle(BlockEntity parent)
    {
        Reset();

        var parentPos = BoardEntity.SpawnPosition;

        var (sucess, entity) = OperatingBlocksEntity.TrySpawnSingle(
            parent,
            parentPos,
            _board.Model
        );

        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Spawn);
        }

        _entity = entity;

        Parent = NBlock.Create(parent);
        _board.AddAsBoardElement(Parent);
        Parent.Position = NBoard.GetRealPosition(parentPos);

        Parent.Outlined = true;
        _board.BringToFront(Parent);

        var anim = Parent.PlaySpawnAnimeAsync();
        return OperationResult.Succeeded(anim, OperationType.Spawn);
    }

    private async Task ShiftRotateAnim(
        RotateDirection direction,
        float duration,
        Vector2I preParentPos,
        Vector2I preChildPos,
        bool pivotIsChild
    )
    {
        var task1 = NormalRotateAnim(direction, duration, preParentPos, preChildPos, pivotIsChild);

        var pivotPos = pivotIsChild ? preChildPos : preParentPos;
        var moverPos = pivotIsChild ? preParentPos : preChildPos;
        var relativePos = (Vector2)(moverPos - pivotPos) * NBlock.BaseSize;

        var tween = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        var pivotHandler = pivotIsChild ? Child.AddOffset() : Parent.AddOffset();
        var moverHandler = pivotIsChild ? Parent.AddOffset() : Child.AddOffset();

        tween.TweenMethod(
            Callable.From<Vector2>(v =>
            {
                pivotHandler.Val = v;
                moverHandler.Val = v;
            }),
            Vector2.Zero,
            relativePos,
            duration
        );

        var task2 = tween.WaitForFinished();
        await Task.WhenAll(task1, task2);
        pivotHandler.Apply();
        moverHandler.Apply();
    }

    private async Task NormalRotateAnim(
        RotateDirection direction,
        float duration,
        Vector2I preParentPos,
        Vector2I preChildPos,
        bool pivotIsChild
    )
    {
        var tween = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);

        var pivotPos = pivotIsChild ? preChildPos : preParentPos;
        var moverPos = pivotIsChild ? preParentPos : preChildPos;

        var relativePos = (Vector2)(moverPos - pivotPos) * NBlock.BaseSize;
        var moverHandler = pivotIsChild ? Parent.AddOffset() : Child.AddOffset();

        var targetDeg = direction switch
        {
            RotateDirection.CW => 90,
            RotateDirection.ACW => -90,
            _ => throw new ArgumentException("無効な回転方向", nameof(direction)),
        };

        tween.TweenMethod(
            Callable.From<float>(deg =>
            {
                var rad = Mathf.DegToRad(deg);
                var rotatedOffset = relativePos.Rotated(rad);
                var targetOffset = rotatedOffset - relativePos;

                moverHandler.Val = targetOffset;
            }),
            0f,
            targetDeg,
            duration
        );

        await tween.WaitForFinished();
        moverHandler.Apply();
    }

    private void Reset()
    {
        _entity = null;
        Parent = null;
        Child = null;
    }

    private bool CheckExistBlocks()
    {
        var exist = _entity != null;
        if (!exist)
        {
            Log.Warn("まだスポーンしていない", 1);
        }
        return exist;
    }
    #endregion
}

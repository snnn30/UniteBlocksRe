using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.OperatingBlocks;
using UniteBlocksRe.Nodes.PlayScreen.PlayerScene.Operation;
using UniteBlocksRe.src.Nodes.NodeExtensions;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene;

public partial class NOperationItem : Node
{
    public OperatingBlocksEntity? Model { get; private set; }
    private NBoard _board = null!;

    public NBlock? Parent { get; private set; }
    public NBlock? Child { get; private set; }

    public void Init(NBoard board)
    {
        _board = board;
    }

    #region Spawn
    public OperationResult Spawn(BlockEntity parent, BlockEntity? child = null)
    {
        Reset();

        if (!TrySpawnEntity(parent, child, out var entity))
        {
            return OperationResult.Failed(OperationType.Spawn);
        }

        Model = entity;
        Parent = CreateBlock(parent, entity.ParentPos);

        if (child != null)
        {
            Child = CreateBlock(child, entity.ChildPos);
        }

        Parent.Outlined = true;
        _board.BringToFront(Parent);

        var anim =
            Child != null
                ? Task.WhenAll(Parent.PlaySpawnAnimeAsync(), Child.PlaySpawnAnimeAsync())
                : Parent.PlaySpawnAnimeAsync();

        return OperationResult.Succeeded(anim, OperationType.Spawn);
    }

    private bool TrySpawnEntity(
        BlockEntity parent,
        BlockEntity? child,
        [NotNullWhen(true)] out OperatingBlocksEntity? entity
    )
    {
        if (child == null)
        {
            return OperatingBlocksEntity.TrySpawnSingle(parent, _board.Model, out entity);
        }

        return OperatingBlocksEntity.TrySpawnDouble(parent, child, _board.Model, out entity);
    }

    private NBlock CreateBlock(BlockEntity entity, Vector2I pos)
    {
        var block = NBlock.Create(entity);

        _board.AddBlockAsChild(block);
        block.Position = NBoard.GetRealPosition(pos);

        return block;
    }
    #endregion

    public OperationResult Settle()
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Settle);
        }
        if (Model.IsHalfUp)
        {
            Log.Warn("マスの半分の高さにある");
            return OperationResult.Failed(OperationType.Settle);
        }

        var task = new Func<NBlock, NBlock?, Task>(
            async (parent, child) =>
            {
                var parentAnim = _board.SetOnBoardAsync(parent, Model.ParentPos);
                var childAnim =
                    child != null
                        ? _board.SetOnBoardAsync(child, Model.ChildPos)
                        : Task.CompletedTask;
                await Task.WhenAll(parentAnim, childAnim);
                parent.Outlined = false;
            }
        )(Parent, Child);

        Reset();
        return OperationResult.Succeeded(task, OperationType.Settle);
    }

    public OperationResult Move(MoveDirection direction, float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Move);
        }

        var sucess = Model.TryMove(direction);
        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Move);
        }

        var task = new Func<Task>(async () =>
        {
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);

            var currentParent = Parent;
            var currentChild = Child;
            var preVec = Vector2.Zero;
            tween.TweenMethod(
                Callable.From<Vector2>(v =>
                {
                    var diff = v - preVec;
                    currentParent.Position += diff;
                    if (currentChild != null)
                    {
                        currentChild.Position += diff;
                    }
                    preVec = v;
                }),
                Vector2.Zero,
                (Vector2)direction.ToVector2I() * NBlock.BaseSize,
                duration
            );

            await tween.WaitForFinished();
        })();

        return OperationResult.Succeeded(task, OperationType.Move);
    }

    public OperationResult Drop(float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Drop);
        }

        var sucess = Model.TryDrop();
        if (!sucess)
        {
            return OperationResult.Failed(OperationType.Drop);
        }

        var task = new Func<Task>(async () =>
        {
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In);

            var currentParent = Parent;
            var currentChild = Child;
            var preVec = Vector2.Zero;
            tween.TweenMethod(
                Callable.From<Vector2>(v =>
                {
                    var diff = v - preVec;
                    currentParent.Position += diff;
                    if (currentChild != null)
                    {
                        currentChild.Position += diff;
                    }
                    preVec = v;
                }),
                Vector2.Zero,
                Vector2.Down * NBlock.BaseSize * 0.5f,
                duration
            );

            await tween.WaitForFinished();
        })();

        return OperationResult.Succeeded(task, OperationType.Drop);
    }

    #region Rotate
    public OperationResult Rotate(RotateDirection direction, float duration)
    {
        if (!CheckExistBlocks())
        {
            return OperationResult.Failed(OperationType.Rotate);
        }

        var preParentPos = Model.ParentPos;
        var preChildPos = Model.ChildPos;

        var result = Model.TryRotate(direction);

        if (result.Success && !result.IsShift)
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
        else if (result.Success && result.IsShift)
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

        var currentParent = Parent!;
        var currentChild = Child!;
        var preVec = Vector2.Zero;
        tween.TweenMethod(
            Callable.From<Vector2>(v =>
            {
                var diff = v - preVec;
                currentParent.Position += diff;
                currentChild.Position += diff;
                preVec = v;
            }),
            Vector2.Zero,
            relativePos,
            duration
        );

        var task2 = tween.WaitForFinished();
        await Task.WhenAll(task1, task2);
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
        var mover = pivotIsChild ? Parent! : Child!;

        var targetDeg = direction switch
        {
            RotateDirection.CW => 90,
            RotateDirection.ACW => -90,
            _ => throw new ArgumentException("無効な回転方向", nameof(direction)),
        };

        var preVec = Vector2.Zero;
        tween.TweenMethod(
            Callable.From<float>(deg =>
            {
                var rad = Mathf.DegToRad(deg);
                var rotatedOffset = relativePos.Rotated(rad);
                var targetOffset = rotatedOffset - relativePos;
                var diff = targetOffset - preVec;
                mover.Position += diff;
                preVec = targetOffset;
            }),
            0f,
            targetDeg,
            duration
        );

        await tween.WaitForFinished();
    }
    #endregion

    private void Reset()
    {
        Model = null;
        Parent = null;
        Child = null;
    }

    [MemberNotNullWhen(true, nameof(Model))]
    [MemberNotNullWhen(true, nameof(Parent))]
    private bool CheckExistBlocks()
    {
        var exist = Model != null;
        if (!exist)
        {
            Log.Warn("まだスポーンしていない", 1);
        }
        return exist;
    }
}

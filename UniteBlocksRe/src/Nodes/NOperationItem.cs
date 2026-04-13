using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Nodes;

public partial class NOperationItem : Node2D
{
    private NBoard _board;

    public NBlock Parent { get; private set; }
    public NBlock Child { get; private set; }
    public Vector2I ParentPos { get; private set; }
    public Vector2I ChildPos { get; private set; }

    private bool _isHalf = false;

    private enum State
    {
        WaitingSpawn,
        Operating,
        Locked,
    }

    private State _state = State.WaitingSpawn;
    private readonly List<Task> _activeTasks = new();

    public void Init(NBoard board)
    {
        _board = board;
    }

    public async Task SpawnBlock(BlockEntity parent, BlockEntity child = null)
    {
        if (_state != State.WaitingSpawn)
        {
            Log.Warn("スポーン待機状態じゃない");
            return;
        }

        Parent = null;
        Child = null;

        foreach (NBlock block in GetChildren())
        {
            block.QueueFree();
        }

        ParentPos = BoardEntity.SpawnPosition;
        (Parent, var parentTask) = _board.SpawnBlock(parent, ParentPos);
        _activeTasks.Add(parentTask);

        var childTask = Task.CompletedTask;
        if (child != null)
        {
            ChildPos = ParentPos + Vector2I.Up;
            (Child, childTask) = _board.SpawnBlock(child, ChildPos);
            _activeTasks.Add(childTask);
        }

        await Task.WhenAll(parentTask, childTask);

        _state = State.Operating;
        _isHalf = false;
    }

    public async Task SetOnBoard()
    {
        if (_state != State.Operating)
        {
            Log.Warn("操作状態じゃない");
            return;
        }

        if (_isHalf)
        {
            Log.Warn("マスの半分の高さにある");
            return;
        }

        _state = State.Locked;

        await Task.WhenAll(_activeTasks);

        var parentTask = _board.SetOnBoard(Parent, ParentPos);
        var childTask = Task.CompletedTask;
        if (Child != null)
        {
            childTask = _board.SetOnBoard(Child, ChildPos);
        }

        await Task.WhenAll(parentTask, childTask);

        Parent = null;
        Child = null;

        _state = State.WaitingSpawn;
        _isHalf = false;
    }

    public (bool Sucess, Task Task) DropLinear() =>
        Drop(0.01f, Tween.TransitionType.Linear, Tween.EaseType.In);

    public (bool Sucess, Task Task) DropSudden() =>
        Drop(0.01f, Tween.TransitionType.Quart, Tween.EaseType.Out);

    public (bool Sucess, Task Task) MoveLeft() =>
        Move(Vector2I.Left, 0.03f, Tween.TransitionType.Sine, Tween.EaseType.InOut);

    public (bool Sucess, Task Task) MoveRight() =>
        Move(Vector2I.Right, 0.03f, Tween.TransitionType.Sine, Tween.EaseType.InOut);

    private (bool Sucess, Task task) Drop(
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        if (_state != State.Operating)
        {
            return (false, Task.CompletedTask);
        }
        if (!CanDrop())
        {
            return (false, Task.CompletedTask);
        }

        if (!_isHalf)
        {
            ParentPos += Vector2I.Down;
            ChildPos += Vector2I.Down;
        }

        var task = DoAnimation();
        RegistTask(task);
        _isHalf = !_isHalf;
        return (true, task);

        bool CanDrop()
        {
            if (_isHalf)
            {
                return true;
            }

            var targetParentPos = ParentPos + Vector2I.Down;
            var canPlace = _board.Model.CanPlace(targetParentPos, Parent.Model);
            if (Child is not null)
            {
                var targetChildPos = ChildPos + Vector2I.Down;
                canPlace &= _board.Model.CanPlace(targetChildPos, Child.Model);
            }

            return canPlace;
        }
        Task DoAnimation()
        {
            var tween = CreateTween().SetTrans(trans).SetEase(ease);
            var sum = Vector2.Zero;
            tween.TweenMethod(
                Callable.From<Vector2>(val =>
                {
                    var diff = val - sum;
                    Parent.Position += diff;
                    if (Child != null)
                    {
                        Child.Position += diff;
                    }
                    sum = val;
                }),
                Vector2.Zero,
                Vector2.Down * NBlock.BaseSize * 0.5f,
                duration
            );

            return tween.WaitForFinished();
        }
    }

    private (bool Sucess, Task Task) Move(
        Vector2I direction,
        float duration,
        Tween.TransitionType trans,
        Tween.EaseType ease
    )
    {
        if (_state != State.Operating)
        {
            return (false, Task.CompletedTask);
        }
        if (!CanMove())
        {
            return (false, Task.CompletedTask);
        }
        ParentPos += direction;
        ChildPos += direction;

        var task = DoAnimation();
        RegistTask(task);
        return (true, task);

        bool CanMove()
        {
            var targetParentPos = ParentPos + direction;
            var canPlace = _board.Model.CanPlace(targetParentPos, Parent.Model);
            canPlace &=
                _board.Model.CanPlace(targetParentPos + Vector2I.Up, Parent.Model) || !_isHalf;

            if (Child is not null)
            {
                var targetChildPos = ChildPos + direction;
                canPlace &= _board.Model.CanPlace(targetChildPos, Child.Model);
                canPlace &=
                    _board.Model.CanPlace(targetChildPos + Vector2I.Up, Child.Model) || !_isHalf;
            }

            return canPlace;
        }
        Task DoAnimation()
        {
            var tween = CreateTween().SetTrans(trans).SetEase(ease);
            var sum = Vector2.Zero;
            tween.TweenMethod(
                Callable.From<Vector2>(val =>
                {
                    var diff = val - sum;
                    Parent.Position += diff;
                    if (Child != null)
                    {
                        Child.Position += diff;
                    }
                    sum = val;
                }),
                Vector2.Zero,
                (Vector2)direction * NBlock.BaseSize,
                duration
            );

            return tween.WaitForFinished();
        }
    }

    public (bool Sucess, Task Task) Rotate(bool clockWise)
    {
        if (_state != State.Operating)
        {
            return (false, Task.CompletedTask);
        }
        if (Child is null)
        {
            return (false, Task.CompletedTask);
        }
        if (CanRotate() is not (true, var targetChildPos))
        {
            return (false, Task.CompletedTask);
        }

        ChildPos = targetChildPos;

        var task = DoAnimation();
        RegistTask(task);
        return (true, task);

        (bool CanRotate, Vector2I TargetChildPos) CanRotate()
        {
            var relativePos = ChildPos - ParentPos;
            var rotatedRelative = clockWise
                ? new Vector2I(-relativePos.Y, relativePos.X)
                : new Vector2I(relativePos.Y, -relativePos.X);
            var targetChildPos = ParentPos + rotatedRelative;

            var canPlace = _board.Model.CanPlace(targetChildPos, Child.Model);
            canPlace &=
                _board.Model.CanPlace(targetChildPos + Vector2I.Up, Child.Model) || !_isHalf;
            if (canPlace is false)
            {
                return (false, default);
            }
            return (true, targetChildPos);
        }
        Task DoAnimation()
        {
            var tween = CreateTween()
                .SetTrans(Tween.TransitionType.Quart)
                .SetEase(Tween.EaseType.Out);
            var sum = 0f;
            tween.TweenMethod(
                Callable.From<float>(deg =>
                {
                    var diff = deg - sum;
                    var radDiff = Mathf.DegToRad(diff);
                    var relativePos = Child.Position - Parent.Position;
                    Child.Position = Parent.Position + relativePos.Rotated(radDiff);
                    sum = deg;
                }),
                0f,
                clockWise ? 90f : -90f,
                0.2f
            );

            return tween.WaitForFinished();
        }
    }

    private void RegistTask(Task task)
    {
        _activeTasks.Add(task);

        _ = task.ContinueWith(_ =>
        {
            _activeTasks.Remove(task);
        });
    }
}

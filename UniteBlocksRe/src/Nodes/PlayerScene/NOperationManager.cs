using System;
using System.Threading.Tasks;
using Godot;
using R3;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;
using UniteBlocksRe.src.Nodes.PlayerScene;

namespace UniteBlocksRe.Nodes;

public partial class NOperationManager : Node
{
    private NOperationItem _item;

    private bool _activeInput = false;
    private bool _activeAutoDrop = false;

    private Timer _maxLockTimer; // 無限上昇を防ぐためのタイマー 最低高度に達するたびにリセット
    private Timer _idleTimer; // 一定時間動かない時に自動で設置するためのタイマー
    private Timer _initialDelayTimer;
    private int _maxAltitude;
    private TaskCompletionSource _endOperationSignal;

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null)
    {
        return _item.Spawn(parent, child);
    }

    public async Task StartRun()
    {
        CompositeDisposable operationDisposable = [];

        SubscribeDropInput(operationDisposable);
        SubscribeMoveInput(operationDisposable);
        SubscribeRotateInput(operationDisposable);

        _endOperationSignal = new TaskCompletionSource();
        _maxAltitude = BoardEntity.SpawnPosition.Y;
        _maxLockTimer.Start();
        _activeInput = true;
        _activeAutoDrop = false;
        _initialDelayTimer.Start();

        await _endOperationSignal.Task;
        operationDisposable.Dispose();

        await _item.Settle().Task;
    }

    public void Init(NBoard board)
    {
        _item.Init(board);
    }

    public override void _Ready()
    {
        _item = GetNode<NOperationItem>("%OperationItem");

        _maxLockTimer = new Timer { OneShot = true, WaitTime = 3f };
        AddChild(_maxLockTimer);

        _idleTimer = new Timer { OneShot = true, WaitTime = 0.6f };
        AddChild(_idleTimer);

        _initialDelayTimer = new Timer { OneShot = true, WaitTime = 1.2f };
        AddChild(_initialDelayTimer);

        _maxLockTimer.Timeout += () =>
        {
            _activeInput = false;
        };
        _idleTimer.Timeout += () =>
        {
            EndOperation();
        };
        _initialDelayTimer.Timeout += () => _activeAutoDrop = true;
    }

    private void EndOperation()
    {
        _activeInput = false;
        _activeAutoDrop = false;
        _maxLockTimer.Stop();
        _idleTimer.Stop();
        _initialDelayTimer.Stop();
        _endOperationSignal.TrySetResult();
    }

    private void SubscribeDropInput(CompositeDisposable disposables)
    {
        var dropInput = Observable
            .EveryUpdate()
            .Select(_ => _activeInput && Input.IsActionPressed("down"))
            .DistinctUntilChanged();

        dropInput
            .Select(isPressed =>
            {
                if (isPressed)
                {
                    return Observable.FromAsync(async ct =>
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            var result = ExecuteDrop(false, 0.04f);
                            await Task.Delay(TimeSpan.FromSeconds(0.03f));
                            if (!result.Sucess)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(0.05f), ct);
                            }
                        }
                    });
                }
                else
                {
                    return Observable
                        .FromEvent<int>(
                            h => NBeatManager.Instance.OnBeat += h,
                            h => NBeatManager.Instance.OnBeat -= h
                        )
                        //並列に合成する
                        .SelectMany(_ =>
                            Observable.FromAsync(async ct =>
                            {
                                if (_activeAutoDrop)
                                {
                                    var result = ExecuteDrop(true, 0.1f);
                                    await result.Task;
                                }
                            })
                        );
                }
            })
            .Switch()
            .Subscribe()
            .AddTo(disposables);

        OperationResult ExecuteDrop(bool isAutoDrop, float duration)
        {
            var result = _item.Drop(duration);
            StopIdleTimer(result); // sucessならidleタイマー止まる
            if (result.Sucess)
            {
                if (!isAutoDrop)
                {
                    _initialDelayTimer.ForceTimeout();
                }
                if (_item.ParentPos.Y > _maxAltitude)
                {
                    _maxAltitude = _item.ParentPos.Y;
                    _maxLockTimer.Start();
                }
            }
            else if (_idleTimer.IsStopped())
            {
                _idleTimer.Start();
            }
            else
            {
                if (!isAutoDrop)
                {
                    _idleTimer.ForceTimeout();
                }
            }
            return result;
        }
    }

    private void SubscribeRotateInput(CompositeDisposable disposables)
    {
        var rotateInput = Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (!_activeInput)
                {
                    return Vector2I.Zero;
                }
                var right = Input.IsActionPressed("rotate_right");
                var left = Input.IsActionPressed("rotate_left");

                if (right && !left)
                {
                    return Vector2I.Right;
                }
                if (!right && left)
                {
                    return Vector2I.Left;
                }
                return Vector2I.Zero;
            })
            .DistinctUntilChanged();

        rotateInput
            .Select(dir =>
            {
                if (dir == Vector2I.Zero)
                {
                    return Observable.Empty<Unit>();
                }

                return Observable.FromAsync(async ct =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        (var sucess, var task) = ExecuteRotate(dir);
                        if (sucess)
                        {
                            await task;
                            return;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(0.05f), ct);
                    }
                });
            })
            .Switch()
            .Subscribe()
            .AddTo(disposables);

        OperationResult ExecuteRotate(Vector2I dir)
        {
            const float duration = 0.2f;

            if (dir == Vector2I.Left)
            {
                var result = _item.Rotate(RotationDirection.ACW, duration);
                StopIdleTimer(result);
                return result;
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.Rotate(RotationDirection.CW, duration);
                StopIdleTimer(result);
                return result;
            }
            else
            {
                Log.Error($"想定していない方向 {dir}");
                return default;
            }
        }
    }

    private void SubscribeMoveInput(CompositeDisposable disposables)
    {
        var moveInput = Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (!_activeInput)
                {
                    return Vector2I.Zero;
                }
                var right = Input.IsActionPressed("right");
                var left = Input.IsActionPressed("left");

                if (right && !left)
                {
                    return Vector2I.Right;
                }
                if (!right && left)
                {
                    return Vector2I.Left;
                }
                return Vector2I.Zero;
            })
            .DistinctUntilChanged(); //値が変化した時だけ通す

        moveInput
            .Select(dir =>
            {
                if (dir == Vector2I.Zero)
                {
                    return Observable.Empty<Unit>(); //通知しない
                }
                // 非同期処理をストリームとして扱う
                return Observable.FromAsync(async ct =>
                {
                    var wasLastMoveSucess = false;
                    while (!ct.IsCancellationRequested)
                    {
                        (var sucess, var task) = ExecuteMove(dir);
                        if (sucess)
                        {
                            var delayTime = wasLastMoveSucess ? 0.01f : 0.1f;
                            await task;
                            await Task.Delay(TimeSpan.FromSeconds(delayTime), ct);
                            wasLastMoveSucess = true;
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(0.05f), ct);
                            wasLastMoveSucess = false;
                        }
                    }
                });
            })
            .Switch() //新しいストリームが届くと古いものを破棄する
            .Subscribe() //実際の処理はSelectだが、Subscribeしないとそこまでの処理も一切行われない
            .AddTo(disposables);

        OperationResult ExecuteMove(Vector2I dir)
        {
            const float duration = 0.06f;

            if (dir == Vector2I.Left)
            {
                var result = _item.Move(MoveDirection.Left, duration);
                StopIdleTimer(result);
                return result;
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.Move(MoveDirection.Right, duration);
                StopIdleTimer(result);
                return result;
            }
            else
            {
                Log.Error($"想定していない方向 {dir}");
                return default;
            }
        }
    }

    private void StopIdleTimer(OperationResult result)
    {
        if (result.Sucess)
        {
            _idleTimer.Stop();
        }
    }
}

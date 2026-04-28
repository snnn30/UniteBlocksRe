using System;
using System.Threading.Tasks;
using Godot;
using R3;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Logging;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;
using UniteBlocksRe.src.Nodes.PlayerScene;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.Nodes;

public partial class NOperationManager : Node
{
    private readonly Subject<OperationResult> _onOperationExecuted = new();
    public Observable<OperationResult> OnOperationExecuted => _onOperationExecuted;
    private readonly Subject<OperatingBlocksEntity> _onSpawn = new();
    public Observable<OperatingBlocksEntity> OnSpawn => _onSpawn;

    public NOperationItem Item { get; private set; }
    private NBombGauge _bombGauge;
    private IOperationInputSource _inputSource;

    private bool _activeInput = false;
    private bool _activeAutoDrop = false;

    private Timer _maxLockTimer; // 無限上昇を防ぐためのタイマー 最低高度に達するたびにリセット
    private Timer _idleTimer; // 一定時間動かない時に自動で設置するためのタイマー
    private Timer _initialDelayTimer;
    private int _maxAltitude;
    private TaskCompletionSource _endOperationSignal;

    private const float CheckInterval = 0.05f;

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null)
    {
        var result = Item.Spawn(parent, child);
        _onOperationExecuted.OnNext(result);
        _onSpawn.OnNext(Item.Model);
        return result;
    }

    public async Task StartRun()
    {
        CompositeDisposable operationDisposable = [];

        SubscribeDropInput(_inputSource, operationDisposable);
        SubscribeMoveInput(_inputSource, operationDisposable);
        SubscribeRotateInput(_inputSource, operationDisposable);

        _endOperationSignal = new TaskCompletionSource();
        _maxAltitude = BoardEntity.SpawnPosition.Y;
        _maxLockTimer.Start();
        _activeInput = true;
        _activeAutoDrop = false;
        _initialDelayTimer.Start();

        await _endOperationSignal.Task;
        operationDisposable.Dispose();

        var result = Item.Settle();
        _onOperationExecuted.OnNext(result);
        await result.Task;
    }

    public void Init(IPlayerContext context)
    {
        Item.Init(context.Board);
        _bombGauge = context.BombGauge;
        _inputSource = context.InputSource;

        SubscribeSwitchInput(_inputSource);
    }

    public override void _Ready()
    {
        Item = GetNode<NOperationItem>("%OperationItem");

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

    private void SubscribeSwitchInput(IOperationInputSource source)
    {
        source
            .SwitchBomb.Subscribe(_ =>
            {
                _bombGauge.TrySetBombActive(!_bombGauge.IsBombActive);
            })
            .AddTo(this);
    }

    private void SubscribeDropInput(IOperationInputSource source, CompositeDisposable disposables)
    {
        const float AutoDropDuration = 0.1f;
        const float ManualDropDuration = 0.03f;

        var dropInput = Observable
            .EveryUpdate()
            .Select(_ => _activeInput && source.IsDropActiveState.CurrentValue)
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
                            var result = ExecuteDrop(false, ManualDropDuration + 0.01f); // 滑らかな演出のため演出に少し時間をかける
                            await Task.Delay(TimeSpan.FromSeconds(ManualDropDuration), ct);
                            if (!result.Sucess)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(CheckInterval), ct);
                            }
                        }
                    });
                }
                else
                {
                    return NBeatManager
                        .Instance.OnBeat.Where(_ => _activeAutoDrop)
                        .SelectMany(_ =>
                            Observable.FromAsync(async _ =>
                                await ExecuteDrop(true, AutoDropDuration).Task
                            )
                        );
                }
            })
            .Switch()
            .Subscribe()
            .AddTo(disposables);

        OperationResult ExecuteDrop(bool isAutoDrop, float duration)
        {
            var result = Item.Drop(duration);
            _onOperationExecuted.OnNext(result);
            StopIdleTimer(result); // sucessならidleタイマー止まる
            if (result.Sucess)
            {
                if (!isAutoDrop) // 待機状態の時に手動落下が来たら自動落下を開始する
                {
                    _initialDelayTimer.ForceTimeout();
                }
                if (Item.ParentPos.Y > _maxAltitude) // 最低高度更新でmaxLockTimerをリセット
                {
                    _maxAltitude = Item.ParentPos.Y;
                    _maxLockTimer.Start();
                }
            }
            else
            {
                if (_idleTimer.IsStopped()) // 落下失敗でidleTimerスタート
                {
                    _idleTimer.Start();
                }
                else if (!isAutoDrop) // 手動落下失敗で即設置
                {
                    _idleTimer.ForceTimeout();
                }
            }

            return result;
        }
    }

    private void SubscribeRotateInput(IOperationInputSource source, CompositeDisposable disposables)
    {
        const float RotateDuration = 0.2f;

        var rotateInput = Observable
            .EveryUpdate()
            .Select(_ =>
                _activeInput ? source.RotateDirectionState.CurrentValue : RotateDirection.None
            )
            .DistinctUntilChanged();

        rotateInput
            .Select(dir =>
            {
                if (dir == RotateDirection.None)
                {
                    return Observable.Empty<Unit>();
                }

                return Observable.FromAsync(async ct =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        (var sucess, var task, var type) = ExecuteRotate(dir, RotateDuration);
                        if (sucess)
                        {
                            await task;
                            return;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(CheckInterval), ct);
                    }
                });
            })
            .Switch()
            .Subscribe()
            .AddTo(disposables);

        OperationResult ExecuteRotate(RotateDirection dir, float duration)
        {
            if (dir == RotateDirection.ACW)
            {
                var result = Item.Rotate(RotateDirection.ACW, duration);
                _onOperationExecuted.OnNext(result);
                StopIdleTimer(result);
                return result;
            }
            else if (dir == RotateDirection.CW)
            {
                var result = Item.Rotate(RotateDirection.CW, duration);
                _onOperationExecuted.OnNext(result);
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

    private void SubscribeMoveInput(IOperationInputSource source, CompositeDisposable disposables)
    {
        const float MoveDuration = 0.06f;
        const float InitialDelay = 0.1f;
        const float RepeatDelay = 0.01f;

        var moveInput = Observable
            .EveryUpdate()
            .Select(_ => _activeInput ? source.MoveDirectionState.CurrentValue : MoveDirection.None)
            .DistinctUntilChanged();

        moveInput
            .Select(dir =>
            {
                if (dir == MoveDirection.None)
                {
                    return Observable.Empty<Unit>(); //通知しない
                }
                // 非同期処理をストリームとして扱う
                return Observable.FromAsync(async ct =>
                {
                    var wasLastMoveSucess = false;
                    while (!ct.IsCancellationRequested)
                    {
                        (var sucess, var task, var type) = ExecuteMove(dir, MoveDuration);
                        if (sucess)
                        {
                            var delayTime = wasLastMoveSucess ? RepeatDelay : InitialDelay;
                            await task;
                            await Task.Delay(TimeSpan.FromSeconds(delayTime), ct);
                            wasLastMoveSucess = true;
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(CheckInterval), ct);
                            wasLastMoveSucess = false;
                        }
                    }
                });
            })
            .Switch() //新しいストリームが届くと古いものを破棄する
            .Subscribe() //実際の処理はSelectだが、Subscribeしないとそこまでの処理も一切行われない
            .AddTo(disposables);

        OperationResult ExecuteMove(MoveDirection dir, float duration)
        {
            if (dir == MoveDirection.Left)
            {
                var result = Item.Move(MoveDirection.Left, duration);
                _onOperationExecuted.OnNext(result);
                StopIdleTimer(result);
                return result;
            }
            else if (dir == MoveDirection.Right)
            {
                var result = Item.Move(MoveDirection.Right, duration);
                _onOperationExecuted.OnNext(result);
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

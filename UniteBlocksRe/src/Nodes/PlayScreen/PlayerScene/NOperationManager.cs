using System;
using System.Threading.Tasks;
using Godot;
using R3;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.OperatingBlocks;
using UniteBlocksRe.Nodes.PlayScreen.PlayerScene.Operation;
using UniteBlocksRe.src.Nodes.NodeExtensions;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene;

public partial class NOperationManager : Node
{
    public Observable<OperationResult> OnOperationExecuted => _onOperationExecuted;
    public NOperationItem Item { get; private set; }

    private readonly Subject<OperationResult> _onOperationExecuted = new();

    private IPlayScreen _screen;
    private PlayerSide _playerSide;

    private bool _activeInput = false;
    private bool _activeAutoDrop = false;

    private Timer _maxLockTimer; // 無限上昇を防ぐためのタイマー 最低高度に達するたびにリセット
    private Timer _idleTimer; // 一定時間動かない時に自動で設置するためのタイマー
    private Timer _initialDelayTimer;
    private int _maxAltitude;
    private TaskCompletionSource _endOperationSignal;

    private const float CheckInterval = 0.05f;

    public async Task Spawn()
    {
        var context = _screen.GetContext(_playerSide);

        BlockEntity parent = null;
        BlockEntity child = null;
        if (context.BombGauge.IsBombActive)
        {
            context.BombGauge.TryUseBomb();
            parent = BlockEntity.CreateBomb();
        }
        else
        {
            var (pair, _) = context.Queue.Dequeue();
            await TimerExtensions.Delay(TimeSpan.FromSeconds(0.2f));
            parent = pair.Parent;
            child = pair.Child;
        }

        var result = Item.Spawn(parent, child);

        _onOperationExecuted.OnNext(result);

        await result.Task;
    }

    public async Task StartRun()
    {
        var context = _screen.GetContext(_playerSide);

        CompositeDisposable operationDisposable = [];
        operationDisposable.AddTo(this);

        SubscribeDropInput(context.InputSource, operationDisposable);
        SubscribeMoveInput(context.InputSource, operationDisposable);
        SubscribeRotateInput(context.InputSource, operationDisposable);

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

    public void Init(IPlayScreen screen, PlayerSide side)
    {
        _screen = screen;
        _playerSide = side;

        var context = _screen.GetContext(side);

        Item.Init(context.Board);
        context
            .InputSource.SwitchInputState.Subscribe(_ =>
            {
                context.BombGauge.TrySetBombActive(!context.BombGauge.IsBombActive);
            })
            .AddTo(this);
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
        _idleTimer.Timeout += EndOperation;
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

    private void SubscribeDropInput(IOperationInputSource source, CompositeDisposable disposables)
    {
        const float AutoDropDuration = 0.1f;
        const float ManualDropDuration = 0.03f;

        var dropInput = Observable
            .EveryUpdate()
            .Select(_ => _activeInput && source.DropInputState.CurrentValue)
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
                            await TimerExtensions.Delay(
                                TimeSpan.FromSeconds(ManualDropDuration),
                                cancellationToken: ct
                            );
                            if (!result.Sucess)
                            {
                                await TimerExtensions.Delay(
                                    TimeSpan.FromSeconds(CheckInterval),
                                    cancellationToken: ct
                                );
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
            HandleOperationResult(result);
            if (result.Sucess)
            {
                if (!isAutoDrop) // 待機状態の時に手動落下が来たら自動落下を開始する
                {
                    _initialDelayTimer.ForceTimeout();
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
            .Select(_ => _activeInput ? source.RotateInputState.CurrentValue : RotateInput.None)
            .DistinctUntilChanged();

        rotateInput
            .Select(dir =>
            {
                if (dir == RotateInput.None)
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
                        await TimerExtensions.Delay(
                            TimeSpan.FromSeconds(CheckInterval),
                            cancellationToken: ct
                        );
                    }
                });
            })
            .Switch()
            .Subscribe()
            .AddTo(disposables);

        OperationResult ExecuteRotate(RotateInput input, float duration)
        {
            var dir = input == RotateInput.ACW ? RotateDirection.ACW : RotateDirection.CW;
            var result = Item.Rotate(dir, duration);
            HandleOperationResult(result);
            return result;
        }
    }

    private void SubscribeMoveInput(IOperationInputSource source, CompositeDisposable disposables)
    {
        const float MoveDuration = 0.06f;
        const float InitialDelay = 0.16f;
        const float RepeatDelay = 0.01f;

        var moveInput = Observable
            .EveryUpdate()
            .Select(_ => _activeInput ? source.MoveInputState.CurrentValue : MoveInput.None)
            .DistinctUntilChanged();

        moveInput
            .Select(dir =>
            {
                if (dir == MoveInput.None)
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
                            await TimerExtensions.Delay(
                                TimeSpan.FromSeconds(delayTime),
                                cancellationToken: ct
                            );
                            wasLastMoveSucess = true;
                        }
                        else
                        {
                            await TimerExtensions.Delay(
                                TimeSpan.FromSeconds(CheckInterval),
                                cancellationToken: ct
                            );
                            wasLastMoveSucess = false;
                        }
                    }
                });
            })
            .Switch() //新しいストリームが届くと古いものを破棄する
            .Subscribe() //実際の処理はSelectだが、Subscribeしないとそこまでの処理も一切行われない
            .AddTo(disposables);

        OperationResult ExecuteMove(MoveInput input, float duration)
        {
            var dir = input == MoveInput.Left ? MoveDirection.Left : MoveDirection.Right;
            var result = Item.Move(dir, duration);
            HandleOperationResult(result);
            return result;
        }
    }

    private OperationResult HandleOperationResult(OperationResult result)
    {
        _onOperationExecuted.OnNext(result);
        if (result.Sucess)
        {
            _idleTimer.Stop();

            if (Item.Model.ParentPos.Y > _maxAltitude) // 最低高度更新でmaxLockTimerをリセット
            {
                _maxAltitude = Item.Model.ParentPos.Y;
                _maxLockTimer.Start();
            }
        }

        return result;
    }
}

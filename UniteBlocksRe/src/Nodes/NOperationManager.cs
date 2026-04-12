using System;
using System.Threading.Tasks;
using Godot;
using R3;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes;

public partial class NOperationManager : Node
{
    private NOperationItem _item;

    public bool ActivateInput { get; set; } = false;
    public bool ActivateAutoDrop { get; set; } = false;

    public void Init(NOperationItem item)
    {
        _item = item;
    }

    public override void _Ready()
    {
        SubscribeMoveInput();
        SubscribeRotateInput();
        SubscribeDropInput();
    }

    private void SubscribeDropInput()
    {
        var dropInput = Observable
            .EveryUpdate()
            .Select(_ => ActivateInput && Input.IsActionPressed("down"))
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
                            await ExecuteDropSudden();
                            await Task.Delay(TimeSpan.FromSeconds(0.005f), ct);
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
                                if (ActivateAutoDrop)
                                {
                                    await ExecuteDropLinear();
                                }
                            })
                        );
                }
            })
            .Switch()
            .Subscribe()
            .AddTo(this);

        async Task ExecuteDropLinear()
        {
            var result = _item.DropLinear();
            await result.Task;
            Log.Debug($"落下 {(result.Sucess ? "成功" : "失敗")}");
        }

        async Task ExecuteDropSudden()
        {
            var result = _item.DropSudden();
            await result.Task;
            Log.Debug($"落下 {(result.Sucess ? "成功" : "失敗")}");
        }
    }

    private void SubscribeRotateInput()
    {
        var rotateInput = Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (!ActivateInput)
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
            .AddTo(this);

        (bool Sucess, Task task) ExecuteRotate(Vector2I dir)
        {
            if (dir == Vector2I.Left)
            {
                var result = _item.Rotate(false);
                Log.Debug($"反時計回り {(result.Sucess ? "成功" : "失敗")}");
                return result;
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.Rotate(true);
                Log.Debug($"時計回り {(result.Sucess ? "成功" : "失敗")}");
                return result;
            }
            else
            {
                Log.Error($"想定していない方向 {dir}");
                return default;
            }
        }
    }

    private void SubscribeMoveInput()
    {
        var moveInput = Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (!ActivateInput)
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
                    await ExecuteMove(dir);
                    await Task.Delay(TimeSpan.FromSeconds(0.16f), ct);
                    while (!ct.IsCancellationRequested)
                    {
                        await ExecuteMove(dir);
                        //0fにすると移動に失敗したときに何万回とループしてしまう
                        await Task.Delay(TimeSpan.FromSeconds(0.01f), ct);
                    }
                });
            })
            .Switch() //新しいストリームが届くと古いものを破棄する
            .Subscribe() //実際の処理はSelectだが、Subscribeしないとそこまでの処理も一切行われない
            .AddTo(this);

        async Task ExecuteMove(Vector2I dir)
        {
            if (dir == Vector2I.Left)
            {
                var result = _item.MoveLeft();
                await result.Task;
                Log.Debug($"左移動 {(result.Sucess ? "成功" : "失敗")}");
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.MoveRight();
                await result.Task;
                Log.Debug($"右移動 {(result.Sucess ? "成功" : "失敗")}");
            }
            else
            {
                Log.Error($"想定していない方向 {dir}");
            }
        }
    }
}

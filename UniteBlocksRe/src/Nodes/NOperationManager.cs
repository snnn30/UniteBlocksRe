using System;
using System.Threading.Tasks;
using Godot;
using R3;
using UniteBlocksRe.Logging;
using UniteBlocksRe.Nodes.OperationItem;

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
                            var result = _item.Drop(false);
                            await result.Task;
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
                                if (ActivateAutoDrop)
                                {
                                    var result = _item.Drop(true);
                                    await result.Task;
                                }
                            })
                        );
                }
            })
            .Switch()
            .Subscribe()
            .AddTo(this);
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

        OperationResult ExecuteRotate(Vector2I dir)
        {
            if (dir == Vector2I.Left)
            {
                var result = _item.Rotate(false);
                return result;
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.Rotate(true);
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
                    var wasLastMoveSucess = false;
                    while (!ct.IsCancellationRequested)
                    {
                        (var sucess, var task) = ExecuteMove(dir);
                        if (sucess)
                        {
                            var delayTime = wasLastMoveSucess ? 0.01f : 0.18f;
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
            .AddTo(this);

        OperationResult ExecuteMove(Vector2I dir)
        {
            if (dir == Vector2I.Left)
            {
                var result = _item.Move(false);
                return result;
            }
            else if (dir == Vector2I.Right)
            {
                var result = _item.Move(true);
                return result;
            }
            else
            {
                Log.Error($"想定していない方向 {dir}");
                return default;
            }
        }
    }
}

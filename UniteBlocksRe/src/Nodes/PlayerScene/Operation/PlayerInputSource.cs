using Godot;
using R3;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public class PlayerInputSource : IOperationInputSource
{
    private readonly ReactiveProperty<MoveDirection> _moveDirection = new(MoveDirection.None);
    private readonly ReactiveProperty<RotateDirection> _rotateDirection = new(RotateDirection.None);
    private readonly ReactiveProperty<bool> _isDropActive = new(false);
    private readonly Subject<Unit> _switchBomb = new();

    public ReadOnlyReactiveProperty<MoveDirection> MoveDirectionState => _moveDirection;
    public ReadOnlyReactiveProperty<RotateDirection> RotateDirectionState => _rotateDirection;
    public ReadOnlyReactiveProperty<bool> IsDropActiveState => _isDropActive;
    public Observable<Unit> SwitchBomb => _switchBomb;

    private readonly CompositeDisposable _disposables = [];

    public PlayerInputSource()
    {
        Observable
            .EveryUpdate()
            .Select(_ => Input.IsActionPressed("down"))
            .Subscribe(active => _isDropActive.Value = active)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (Input.IsActionPressed("left"))
                {
                    return MoveDirection.Left;
                }
                if (Input.IsActionPressed("right"))
                {
                    return MoveDirection.Right;
                }
                return MoveDirection.None;
            })
            .Subscribe(dir => _moveDirection.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (Input.IsActionPressed("rotate_left"))
                {
                    return RotateDirection.ACW;
                }
                if (Input.IsActionPressed("rotate_right"))
                {
                    return RotateDirection.CW;
                }
                return RotateDirection.None;
            })
            .Subscribe(dir => _rotateDirection.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Where(_ => Input.IsActionJustPressed("switch"))
            .Subscribe(_ => _switchBomb.OnNext(Unit.Default))
            .AddTo(_disposables);
    }

    public void UpdateStrategy(BoardEntity board, NBombGauge gauge, BlockQueueEntity queue) { }
}

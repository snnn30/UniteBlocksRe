using Godot;
using R3;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public class PlayerInputSource : IOperationInputSource
{
    private readonly ReactiveProperty<MoveDirection> _moveDirectionState = new(MoveDirection.None);
    private readonly ReactiveProperty<RotationDirection> _rotateDirectionState = new(
        RotationDirection.None
    );
    private readonly ReactiveProperty<bool> _isDropActive = new(false);
    private readonly Subject<Unit> _switchBomb = new();

    public ReadOnlyReactiveProperty<MoveDirection> MoveDirectionState => _moveDirectionState;
    public ReadOnlyReactiveProperty<RotationDirection> RotateDirectionState =>
        _rotateDirectionState;
    public ReadOnlyReactiveProperty<bool> IsDropActive => _isDropActive;
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
            .Subscribe(dir => _moveDirectionState.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (Input.IsActionPressed("rotate_left"))
                {
                    return RotationDirection.ACW;
                }
                if (Input.IsActionPressed("rotate_right"))
                {
                    return RotationDirection.CW;
                }
                return RotationDirection.None;
            })
            .Subscribe(dir => _rotateDirectionState.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Where(_ => Input.IsActionJustPressed("switch"))
            .Subscribe(_ => _switchBomb.OnNext(Unit.Default))
            .AddTo(_disposables);
    }
}

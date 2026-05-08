using Godot;
using R3;

namespace UniteBlocksRe.Nodes.PlayScreen.Operation;

public class PlayerInputSource : IOperationInputSource
{
    private readonly ReactiveProperty<MoveInput> _moveDirection = new(MoveInput.None);
    private readonly ReactiveProperty<RotateInput> _rotateDirection = new(RotateInput.None);
    private readonly ReactiveProperty<bool> _isDropActive = new(false);
    private readonly Subject<Unit> _switchBomb = new();

    public ReadOnlyReactiveProperty<MoveInput> MoveInputState => _moveDirection;
    public ReadOnlyReactiveProperty<RotateInput> RotateInputState => _rotateDirection;
    public ReadOnlyReactiveProperty<bool> DropInputState => _isDropActive;
    public Observable<Unit> SwitchInputState => _switchBomb;

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
                    return MoveInput.Left;
                }
                if (Input.IsActionPressed("right"))
                {
                    return MoveInput.Right;
                }
                return MoveInput.None;
            })
            .Subscribe(dir => _moveDirection.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Select(_ =>
            {
                if (Input.IsActionPressed("rotate_left"))
                {
                    return RotateInput.ACW;
                }
                if (Input.IsActionPressed("rotate_right"))
                {
                    return RotateInput.CW;
                }
                return RotateInput.None;
            })
            .Subscribe(dir => _rotateDirection.Value = dir)
            .AddTo(_disposables);

        Observable
            .EveryUpdate()
            .Where(_ => Input.IsActionJustPressed("switch"))
            .Subscribe(_ => _switchBomb.OnNext(Unit.Default))
            .AddTo(_disposables);
    }
}

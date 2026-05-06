using R3;

namespace UniteBlocksRe.Nodes.PlayScreen.Operation;

public interface IOperationInputSource
{
    ReadOnlyReactiveProperty<MoveInput> MoveInputState { get; }
    ReadOnlyReactiveProperty<RotateInput> RotateInputState { get; }
    ReadOnlyReactiveProperty<bool> DropInputState { get; }
    Observable<Unit> SwitchInputState { get; }
}

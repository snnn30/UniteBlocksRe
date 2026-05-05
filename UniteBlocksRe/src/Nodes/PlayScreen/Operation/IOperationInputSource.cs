using R3;

namespace UniteBlocksRe.Nodes.PlayScreen.Operation;

public interface IOperationInputSource
{
    ReadOnlyReactiveProperty<MoveInput> MoveDirectionState { get; }
    ReadOnlyReactiveProperty<RotateInput> RotateDirectionState { get; }
    ReadOnlyReactiveProperty<bool> IsDropActiveState { get; }
    Observable<Unit> SwitchBomb { get; }
}

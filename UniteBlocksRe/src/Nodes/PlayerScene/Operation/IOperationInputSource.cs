using R3;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public interface IOperationInputSource
{
    ReadOnlyReactiveProperty<MoveInput> MoveDirectionState { get; }
    ReadOnlyReactiveProperty<RotateInput> RotateDirectionState { get; }
    ReadOnlyReactiveProperty<bool> IsDropActiveState { get; }
    Observable<Unit> SwitchBomb { get; }
}

using R3;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public interface IOperationInputSource
{
    ReadOnlyReactiveProperty<MoveDirection> MoveDirectionState { get; }
    ReadOnlyReactiveProperty<RotateDirection> RotateDirectionState { get; }
    ReadOnlyReactiveProperty<bool> IsDropActiveState { get; }
    Observable<Unit> SwitchBomb { get; }
}

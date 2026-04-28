using R3;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public interface IOperationInputSource
{
    ReadOnlyReactiveProperty<MoveDirection> MoveDirectionState { get; }
    ReadOnlyReactiveProperty<RotateDirection> RotateDirectionState { get; }
    ReadOnlyReactiveProperty<bool> IsDropActiveState { get; }
    Observable<Unit> SwitchBomb { get; }

    void UpdateStrategy(BoardEntity board, NBombGauge gauge, BlockQueueEntity queue);
}

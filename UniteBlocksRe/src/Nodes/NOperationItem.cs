using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Nodes.OperationItem;
using UniteBlocksRe.Nodes.OperationItem.Handlers;

namespace UniteBlocksRe.Nodes;

public partial class NOperationItem : Node2D
{
    private OperationContext _context;

    public void Init(NBoard board)
    {
        var state = new OperationState(
            null,
            null,
            Vector2I.Zero,
            Vector2I.Zero,
            false,
            OperationPhase.WaitingSpawn
        );
        _context = new(board, this, state);
    }

    public OperationResult Settle() => SettleHandler.Settle(_context);

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null) =>
        SpawnHandler.Spawn(_context, parent, child);

    public OperationResult Rotate(bool isCW) => RotateHandler.Rotate(_context, isCW);

    public OperationResult Move(bool isRight) => MoveHandler.Move(_context, isRight);

    public OperationResult Drop(bool isSingle) => DropHandler.Drop(_context, isSingle);
}

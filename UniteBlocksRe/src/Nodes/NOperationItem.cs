using Godot;
using UniteBlocksRe.Models.Entities;
using UniteBlocksRe.Nodes.OperationItem;
using UniteBlocksRe.Nodes.OperationItem.Handlers;

namespace UniteBlocksRe.Nodes;

public partial class NOperationItem : Node
{
    private OperationContext _context;

    public Vector2I ParentPos => _context.ParentPos;
    public Vector2I ChildPos => _context.ChildPos;

    public void Init(NBoard board)
    {
        _context = new(board);
    }

    public OperationResult Settle() => SettleHandler.Settle(_context);

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null) =>
        SpawnHandler.Spawn(_context, parent, child);

    public OperationResult Rotate(bool isCW, float duration) =>
        RotateHandler.Rotate(_context, isCW, duration);

    public OperationResult Move(bool isRight, float duration) =>
        MoveHandler.Move(_context, isRight, duration);

    public OperationResult Drop(float duration) => DropHandler.Drop(_context, duration);
}

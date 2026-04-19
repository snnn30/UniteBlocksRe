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
        _context = new(board);
    }

    public override void _Process(double delta)
    {
        if (_context == null)
        {
            return;
        }

        var totalOffset = new RealPositions();
        foreach (var offset in _context.Offsets)
        {
            totalOffset.Add(offset);
        }

        if (_context.Parent != null)
        {
            _context.Parent.Position = _context.BasePoasitions.Parent + totalOffset.Parent;
        }
        if (_context.Child != null)
        {
            _context.Child.Position = _context.BasePoasitions.Child + totalOffset.Child;
        }
    }

    public OperationResult Settle() => SettleHandler.Settle(_context);

    public OperationResult Spawn(BlockEntity parent, BlockEntity child = null) =>
        SpawnHandler.Spawn(_context, parent, child);

    public OperationResult Rotate(bool isCW) => RotateHandler.Rotate(_context, isCW);

    public OperationResult Move(bool isRight) => MoveHandler.Move(_context, isRight);

    public OperationResult Drop(bool isSingle) => DropHandler.Drop(_context, isSingle);
}

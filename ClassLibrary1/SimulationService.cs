using System.Collections.Immutable;
using UniteBlocksRe.Domain.BlockOperation;
using UniteBlocksRe.Domain.Common;
using UniteBlocksRe.Domain.Simulations;

namespace UniteBlocksRe.Domain;

public static class SimulationService
{
    public static IReadOnlyList<SimulationResult> SimulateAll(
        OperatingBlockPair initial,
        Board board
    )
    {
        return Explore(
            initial,
            board,
            ImmutableList<BlockOperationStep>.Empty,
            ImmutableHashSet<(Vector2I, Vector2I)>.Empty
        );
    }

    private static IReadOnlyList<SimulationResult> Explore(
        OperatingBlockPair current,
        Board board,
        ImmutableList<BlockOperationStep> steps,
        ImmutableHashSet<(Vector2I, Vector2I)> visited
    )
    {
        var stateKey = (current.ParentPos, current.ChildPos);
        if (visited.Contains(stateKey))
        {
            return Array.Empty<SimulationResult>();
        }

        var results = new List<SimulationResult> { Finalize(current, board, steps) };

        var nextVisited = visited.Add(stateKey);

        foreach (var dir in new[] { MoveDirection.Left, MoveDirection.Right })
        {
            if (current.TryMove(dir, board, out var next))
            {
                var nextSteps = steps.Add(new MoveStep(dir, 1));
                results.AddRange(Explore(next, board, nextSteps, nextVisited));
            }
        }

        foreach (var rotDir in new[] { RotateDirection.CW, RotateDirection.ACW })
        {
            var rotation = current.TryRotate(rotDir, board, out var next);
            if (rotation.Sucess)
            {
                var nextSteps = steps.Add(new RotateStep(rotDir, 1));
                results.AddRange(Explore(next, board, nextSteps, nextVisited));
            }
        }

        return results;
    }

    private static SimulationResult Finalize(
        OperatingBlockPair state,
        Board board,
        ImmutableList<BlockOperationStep> steps
    )
    {
        var movingState = state;
        var dropCount = 0;
        while (movingState.TryDrop(board, out var next))
        {
            movingState = next;
            dropCount++;
        }

        var finalBlockOps = dropCount > 0 ? steps.Add(new DropStep(dropCount)) : steps;

        var currentBoard = board.Place(movingState.ParentPos, movingState.Parent);
        if (movingState.Child != null)
        {
            currentBoard = currentBoard.Place(movingState.ChildPos, movingState.Child);
        }

        var processResult = BoardService.Process(currentBoard);
        var boardOperations = processResult.History;
        currentBoard = processResult.FinalState;

        return new SimulationResult(currentBoard, finalBlockOps, boardOperations);
    }
}

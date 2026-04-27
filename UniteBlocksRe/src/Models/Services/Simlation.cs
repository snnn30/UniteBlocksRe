using System.Collections.Generic;
using Godot;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;
using UniteBlocksRe.src.Models.ValueObjects.Simulation;

namespace UniteBlocksRe.src.Models.Services;

public static class Simlation
{
    public static SimulationBombResult SimulateSetBomb(OperatingBlocksEntity operating)
    {
        var result = new SimulationBombResult();
        var moveClones = GetMoveClones(operating);
        var board = operating.Board;
        foreach (var moveData in moveClones)
        {
            var finalOp = moveData.Entity.ShallowCopy();

            List<OperationStep> operations = [];
            if (moveData.Count > 0)
            {
                operations.Add(new MoveStep(moveData.Direction, moveData.Count));
            }
            var dropCount = 0;
            while (finalOp.TryDrop())
            {
                dropCount++;
            }
            if (dropCount > 0)
            {
                operations.Add(new DropStep(dropCount));
            }
            var simBoard = board.DeepCopy();
            var bombCopy = finalOp.Parent.DeepCopy();
            simBoard.TrySetBlock(finalOp.ParentPos, bombCopy);

            BoardFaller.Fall(simBoard);
            BoardUniter.Unite(simBoard);
            var exploadeResult = BoardExploder.Explode(simBoard, bombCopy);

            result.Results.Add((simBoard, new OperationInstructions(operations), exploadeResult));
        }
        return result;
    }

    public static SimulationBlocksResult SimulateSetBlocks(OperatingBlocksEntity operating)
    {
        var result = new SimulationBlocksResult();
        var visited = new HashSet<(Vector2I, Vector2I)>(); //parent, child
        var moveClones = GetMoveClones(operating);
        var board = operating.Board;

        foreach (var moveData in moveClones)
        {
            var rotationClones = GetRotationClones(moveData.Entity);

            foreach (var rotationData in rotationClones)
            {
                var finalOp = rotationData.Entity.ShallowCopy();

                List<OperationStep> operations = [];
                if (moveData.Count > 0)
                {
                    operations.Add(new MoveStep(moveData.Direction, moveData.Count));
                }
                if (rotationData.Count > 0)
                {
                    operations.Add(new RotateStep(rotationData.Direction, rotationData.Count));
                }

                var dropCount = 0;
                while (finalOp.TryDrop())
                {
                    dropCount++;
                }
                if (dropCount > 0)
                {
                    operations.Add(new DropStep(dropCount));
                }

                if (visited.Add((finalOp.ParentPos, finalOp.ChildPos)))
                {
                    var simBoard = board.DeepCopy();
                    simBoard.TrySetBlock(finalOp.ParentPos, finalOp.Parent.DeepCopy());
                    simBoard.TrySetBlock(finalOp.ChildPos, finalOp.Child.DeepCopy());

                    BoardFaller.Fall(simBoard);
                    BoardUniter.Unite(simBoard);

                    result.Results.Add((simBoard, new OperationInstructions(operations)));
                }
            }
        }

        return result;
    }

    private record struct MoveData(
        OperatingBlocksEntity Entity,
        MoveDirection Direction,
        int Count
    );

    private record struct RotateData(
        OperatingBlocksEntity Entity,
        RotateDirection Direction,
        int Count
    );

    private static List<MoveData> GetMoveClones(OperatingBlocksEntity root)
    {
        var list = new List<MoveData> { new(root.ShallowCopy(), MoveDirection.None, 0) };

        var leftOpe = root.ShallowCopy();
        var leftCount = 0;
        while (leftOpe.TryMove(MoveDirection.Left))
        {
            leftCount++;
            list.Add(new(leftOpe.ShallowCopy(), MoveDirection.Left, leftCount));
        }

        var rightCur = root.ShallowCopy();
        var rightCount = 0;
        while (rightCur.TryMove(MoveDirection.Right))
        {
            rightCount++;
            list.Add(new(rightCur.ShallowCopy(), MoveDirection.Right, rightCount));
        }

        return list;
    }

    private static List<RotateData> GetRotationClones(OperatingBlocksEntity root)
    {
        var list = new List<RotateData> { new(root.ShallowCopy(), RotateDirection.None, 0) };
        var visited = new HashSet<(Vector2I, Vector2I)> { (root.ParentPos, root.ChildPos) };

        var cwOpe = root.ShallowCopy();
        for (var i = 1; i <= 3; i++)
        {
            var (success, _, _) = cwOpe.TryRotate(RotateDirection.CW);
            var pos = (cwOpe.ParentPos, cwOpe.ChildPos);
            if (success && !visited.Contains(pos))
            {
                list.Add(new(cwOpe.ShallowCopy(), RotateDirection.CW, i));
                visited.Add(pos);
            }
            else
            {
                break;
            }
        }

        var acwOpe = root.ShallowCopy();
        for (var i = 1; i <= 3; i++)
        {
            var (success, _, _) = acwOpe.TryRotate(RotateDirection.ACW);
            var pos = (acwOpe.ParentPos, acwOpe.ChildPos);
            if (success && !visited.Contains(pos))
            {
                list.Add(new(acwOpe.ShallowCopy(), RotateDirection.ACW, i));
                visited.Add(pos);
            }
            else
            {
                break;
            }
        }

        return list;
    }
}

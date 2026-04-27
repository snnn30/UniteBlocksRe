using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;

namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public abstract record OperationStep;

public record MoveStep(MoveDirection Direction, int Count) : OperationStep;

public record RotateStep(RotateDirection Direction, int Count) : OperationStep;

public record DropStep(int Count) : OperationStep;

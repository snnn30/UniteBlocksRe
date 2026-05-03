using UniteBlocksRe.Domain.BlockOperation;

namespace UniteBlocksRe.Domain.Simulations;

public abstract record BlockOperationStep;

public sealed record MoveStep(MoveDirection Direction, int Count) : BlockOperationStep;

public sealed record RotateStep(RotateDirection Direction, int Count) : BlockOperationStep;

public sealed record DropStep(int Count) : BlockOperationStep;

using UniteBlocksRe.Domain.Boards.Operations;

namespace UniteBlocksRe.Domain.Simulations;

public sealed record SimulationResult(
    Board FinalState,
    IReadOnlyList<BlockOperationStep> BlockOperations,
    IReadOnlyList<BoardOperationStep> BoardOperations
);

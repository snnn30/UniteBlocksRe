namespace UniteBlocksRe.Domain.Boards.Operations;

public abstract record BoardOperationStep(Board Before, Board After);

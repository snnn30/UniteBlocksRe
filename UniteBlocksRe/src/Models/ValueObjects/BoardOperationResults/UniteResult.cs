using System.Collections.Generic;
using Godot;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

public record UniteResult(List<UniteStep> Steps)
{
    public int ChainCount => Steps.Count;
    public bool HasUnited => Steps.Count > 0;
}

public record UniteStep(
    IReadOnlySet<BlockEntity> RemovedBlocks,
    BlockEntity CreatedBlock,
    Vector2I Position
);

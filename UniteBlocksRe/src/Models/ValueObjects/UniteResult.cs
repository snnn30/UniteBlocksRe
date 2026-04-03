using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Models.ValueObjects;

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

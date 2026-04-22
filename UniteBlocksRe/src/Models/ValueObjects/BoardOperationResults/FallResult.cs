using System.Collections.Generic;
using Godot;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

public record FallResult(List<FallStep> Steps)
{
    public int Count => Steps.Count;
    public bool HasChanged => Steps.Count > 0;
}

public record FallStep(BlockEntity Block, Vector2I From, Vector2I To);

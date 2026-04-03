using System.Collections.Generic;
using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Models.ValueObjects;

public record FallResult(List<FallStep> Steps)
{
    public int Count => Steps.Count;
    public bool HasChanged => Steps.Count > 0;
}

public record FallStep(BlockEntity Block, Vector2I From, Vector2I To);

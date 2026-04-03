using System.Collections.Generic;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Models.ValueObjects;

public record ExplodeResult(List<ExplodeStep> Steps)
{
    public int ChainCount => Steps.Count;
    public bool HasExploded => Steps.Count > 0;
}

public record ExplodeStep(HashSet<BlockEntity> Exploded);

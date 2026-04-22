using System.Collections.Generic;
using System.Linq;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

public record ExplodeResult(List<ExplodeStep> Steps)
{
    public int ChainCount => Steps.Count;
    public bool HasExploded => Steps.Count > 0;

    public override string ToString()
    {
        if (!HasExploded)
        {
            return "ExplodeResult: No Explosion";
        }

        var stepsStr = Steps.Select((step, index) => $"Step [{index}]:\n{step}");
        return $"--- ExplodeResult (Chains: {ChainCount}) ---\n{string.Join("\n", stepsStr)}\n---------------------------";
    }
}

public record ExplodeStep(HashSet<BlockEntity> Exploded)
{
    public override string ToString()
    {
        var list = string.Join("\n  - ", Exploded);
        return $"ExplodeStep:\n  - {list}";
    }
};

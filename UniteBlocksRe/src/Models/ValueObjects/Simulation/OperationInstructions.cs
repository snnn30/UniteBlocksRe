using System.Collections.Generic;

namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public record struct OperationInstructions(List<OperationStep> Steps)
{
    public override string ToString()
    {
        if (Steps == null || Steps.Count == 0)
        {
            return "OperationInstructions { Empty }";
        }

        var stepsText = string.Join(", ", Steps);
        return $"OperationInstructions {{ Steps = [ {stepsText} ] }}";
    }
}

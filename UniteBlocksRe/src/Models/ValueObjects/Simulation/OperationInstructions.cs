using System.Collections.Generic;

namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public record struct OperationInstructions(List<OperationStep> Steps);

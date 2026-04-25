using System.Collections.Generic;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public class SimulationBlocksResult
{
    public List<(BoardEntity Board, OperationInstructions Operations)> Results { get; } = [];
}

using System.Collections.Generic;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

namespace UniteBlocksRe.src.Models.ValueObjects.Simulation;

public class SimulationBombResult
{
    public List<(
        BoardEntity Board,
        OperationInstructions Operations,
        ExplodeResult Explode
    )> Results { get; } = [];
}

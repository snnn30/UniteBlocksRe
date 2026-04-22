using System.Collections.Generic;
using System.Linq;
using Godot;
using UniteBlocksRe.src.Models.Entities;

namespace UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

public record ObstaclePlaceResult(Dictionary<int, ColumnResult> Colmuns)
{
    public bool Placed => Colmuns.Count > 0;
    public int PlacedCount => Colmuns.Values.Sum(c => c.Blocks.Count);
}

public record ColumnResult(List<(BlockEntity block, Vector2I position)> Blocks);

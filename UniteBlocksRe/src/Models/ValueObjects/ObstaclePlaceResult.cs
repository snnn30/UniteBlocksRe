using System.Collections.Generic;
using System.Linq;
using Godot;
using UniteBlocksRe.Models.Entities;

namespace UniteBlocksRe.Models.ValueObjects;

public record ObstaclePlaceResult(Dictionary<int, ColumnResult> Colmuns)
{
    public bool Placed => Colmuns.Count > 0;
    public int PlacedCount => Colmuns.Values.Sum(c => c.Blocks.Count);
}

public record ColumnResult(List<(BlockEntity block, Vector2I position)> Blocks);

using Godot;
using UniteBlocksRe.Extensions;
using UniteBlocksRe.Models.Block;
using UniteBlocksRe.Models.BoardServices;

namespace UniteBlocksRe.Nodes.PlayScreen;

public partial class NObstacleCounter : Node2D
{
    private Label _label;

    private int _score = 0;
    private int _count;
    private int _viewCount;
    public int ViewCount
    {
        get { return _viewCount; }
        private set
        {
            _viewCount = value;
            _label.Text = _viewCount.ToString();
        }
    }
    private readonly float _obstacleRate = 4.5f;

    public override void _Ready()
    {
        _label = GetNode<Label>("%Label");
        ViewCount = 0;
    }

    public void AddCount(ExplodeStep step)
    {
        foreach (var block in step.ExplodedBlocks)
        {
            if (block.Type != BlockType.Normal)
            {
                continue;
            }
            var area = block.Size.GetArea();
            _score += area * area;
        }

        ViewCount = _count + (int)(_score / _obstacleRate);
    }

    public void OnEndExplode()
    {
        _score = 0;
        _count = ViewCount;
    }

    public void SubCount(ObstaclePlaceResult result)
    {
        var placedCount = result.PlacedCount;
        ViewCount -= placedCount;
        _count -= placedCount;
    }
}

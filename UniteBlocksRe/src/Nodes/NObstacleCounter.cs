using Godot;
using UniteBlocksRe.src.Extensions;
using UniteBlocksRe.src.Models.ValueObjects;
using UniteBlocksRe.src.Models.ValueObjects.BoardOperationResults;

namespace UniteBlocksRe.Nodes;

public partial class NObstacleCounter : Node2D
{
    private Label _label;
    private NBoard _board;

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
    private float _obstacleRate = 4.5f;

    public void Init(NBoard board)
    {
        _board = board;
    }

    public override void _Ready()
    {
        _label = GetNode<Label>("%Label");
        ViewCount = 0;
    }

    public void AddCount(ExplodeStep step)
    {
        foreach (var block in step.Exploded)
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

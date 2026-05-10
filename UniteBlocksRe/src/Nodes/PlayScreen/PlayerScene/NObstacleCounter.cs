using System;
using Godot;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene;

public partial class NObstacleCounter : Node2D
{
    private Label _obstacleLabel;
    private Label _turnLabel;
    private Polygon2D _bg;

    private int _obstacleCount;
    public int ObstacleCount
    {
        get { return _obstacleCount; }
        set
        {
            _obstacleCount = Math.Max(0, value);
            _obstacleLabel.Text = _obstacleCount.ToString();

            if (_obstacleCount > 0)
            {
                _bg.Color = new Color(0.9f, 0.1f, 0.1f, 0.7f);
            }
            else
            {
                TurnCount = 0;
                _bg.Color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            }
        }
    }

    private int _turnCount;
    public int TurnCount
    {
        get { return _turnCount; }
        set
        {
            _turnCount = Math.Max(0, value);
            _turnLabel.Text = _turnCount.ToString();

            if (_turnCount > 0)
            {
                _turnLabel.Visible = true;
            }
            else
            {
                _turnLabel.Visible = false;
            }
        }
    }

    public override void _Ready()
    {
        _bg = GetNode<Polygon2D>("%Bg");
        _obstacleLabel = GetNode<Label>("%ObstacleCount");
        _turnLabel = GetNode<Label>("%TurnCount");

        ObstacleCount = 0;
    }
}

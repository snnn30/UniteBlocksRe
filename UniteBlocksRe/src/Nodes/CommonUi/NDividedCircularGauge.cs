using System.Collections.Generic;
using Godot;

namespace UniteBlocksRe.Nodes.CommonUi;

[Tool]
public partial class NDividedCircularGauge : Control
{
    public const int UnitsPerSegment = 10000;

    [ExportGroup("Shape")]
    [Export]
    public int Segments { get; set; } = 8;

    [Export]
    public float Radius { get; set; } = 100f;

    [Export]
    public float InnerRadius { get; set; } = 70f;

    [Export]
    public float SpacingDegrees { get; set; } = 20f;

    [ExportGroup("Colors")]
    [Export]
    public Color BaseColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Export]
    public Color FilledColor { get; set; } = Colors.Cyan;

    [Export]
    public Color ChargingColor { get; set; } = Colors.White;

    [ExportGroup("Outline")]
    [Export]
    public Color OutlineColor { get; set; } = Colors.White;

    [Export]
    public float OutlineWidth { get; set; } = 2.0f;

    [ExportGroup("Value")]
    private int _value = 5000;

    [Export]
    public int Value
    {
        get => _value;
        set { _value = Mathf.Clamp(value, 0, MaxValue); }
    }

    public int MaxValue => Segments * UnitsPerSegment;

    public int ChargedSegments => Value / UnitsPerSegment;

    private float FillRatio => (float)Value / MaxValue;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var totalAngle = Mathf.Tau;
        var spacingRads = Mathf.DegToRad(SpacingDegrees);
        var drawAnglePerSegment = totalAngle / Segments - spacingRads;

        var ratio = FillRatio;

        for (var i = 0; i < Segments; i++)
        {
            var startAngle = i * (totalAngle / Segments) - Mathf.Pi / 2;
            var actualStartAngle = startAngle + (spacingRads / 2.0f);
            var actualEndAngle = actualStartAngle + drawAnglePerSegment;

            var segmentStartValue = (float)i / Segments;
            var segmentEndValue = (float)(i + 1) / Segments;

            DrawSegment(actualStartAngle, actualEndAngle, BaseColor);

            if (ratio >= segmentEndValue - 0.00001f)
            {
                DrawSegment(actualStartAngle, actualEndAngle, FilledColor);
            }
            else if (ratio > segmentStartValue)
            {
                var progress = (ratio - segmentStartValue) / (segmentEndValue - segmentStartValue);
                var currentEndAngle = Mathf.Lerp(actualStartAngle, actualEndAngle, progress);
                DrawSegment(actualStartAngle, currentEndAngle, ChargingColor);
            }

            // 3. アウトライン描画
            DrawSegmentOutline(actualStartAngle, actualEndAngle, OutlineColor);
        }
    }

    private void DrawSegment(float startAngle, float endAngle, Color color)
    {
        var pointsPerArc = 16;
        var points = new Vector2[(pointsPerArc + 1) * 2];
        for (var i = 0; i <= pointsPerArc; i++)
        {
            var angle = Mathf.Lerp(startAngle, endAngle, (float)i / pointsPerArc);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points[i] = dir * Radius;
            points[points.Length - 1 - i] = dir * InnerRadius;
        }
        DrawPolygon(points, [color]);
    }

    private void DrawSegmentOutline(float startAngle, float endAngle, Color color)
    {
        if (OutlineWidth <= 0)
        {
            return;
        }

        var pointsPerArc = 16;
        var points = new List<Vector2>();
        for (var i = 0; i <= pointsPerArc; i++)
        {
            var angle = Mathf.Lerp(startAngle, endAngle, (float)i / pointsPerArc);
            points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius);
        }
        for (var i = pointsPerArc; i >= 0; i--)
        {
            var angle = Mathf.Lerp(startAngle, endAngle, (float)i / pointsPerArc);
            points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * InnerRadius);
        }
        points.Add(points[0]);
        DrawPolyline(points.ToArray(), color, OutlineWidth, true);
    }
}

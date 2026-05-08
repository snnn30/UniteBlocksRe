using Godot;
using UniteBlocksRe.Nodes;

public partial class GameDescriptionScreen : Control
{
    private readonly (string Name, float Time)[] _sections =
    [
        ("基本操作", 0f),
        ("合体", 23f),
        ("ボム", 35f),
        ("防御", 68f),
    ];

    private VideoStreamPlayer _videoPlayer;

    private int _currentSectionIndex;

    private void NextSection()
    {
        _currentSectionIndex++;
        _currentSectionIndex = Mathf.Clamp(_currentSectionIndex, 0, _sections.Length - 1);
        var section = _sections[_currentSectionIndex];
        _videoPlayer.StreamPosition = section.Time;
    }

    private void PreviousSection()
    {
        _currentSectionIndex--;
        _currentSectionIndex = Mathf.Clamp(_currentSectionIndex, 0, _sections.Length - 1);
        var section = _sections[_currentSectionIndex];
        _videoPlayer.StreamPosition = section.Time;
    }

    public override void _Ready()
    {
        _videoPlayer = GetNode<VideoStreamPlayer>("%VideoStreamPlayer");
    }

    public override void _Process(double delta)
    {
        var time = _videoPlayer.StreamPosition;
        for (var i = _sections.Length - 1; i >= 0; i--)
        {
            if (time >= _sections[i].Time)
            {
                _currentSectionIndex = i;
                return;
            }
        }
        _currentSectionIndex = 0;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("left"))
        {
            PreviousSection();
        }
        if (@event.IsActionPressed("right"))
        {
            NextSection();
        }
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            var mainMenuScreen = GD.Load<PackedScene>("res://scenes/screens/main_menu_screen.tscn");
            NGame.Instance.LoadScreen(mainMenuScreen);
        }
    }
}

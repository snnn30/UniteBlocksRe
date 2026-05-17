using Godot;

namespace UniteBlocksRe.Nodes;

public partial class NGameDescriptionScreen : Control
{
    private readonly (string Name, float Time)[] _sections =
    [
        ("基本操作", 0f),
        ("合体", 35f),
        ("ボム", 47f),
        ("防御", 83f),
    ];

    private VideoStreamPlayer _videoPlayer = null!;

    private int _currentSectionIndex;

    public override void _Ready()
    {
        _videoPlayer = GetNode<VideoStreamPlayer>("%VideoStreamPlayer");
    }

    public override void _Process(double delta)
    {
        if (!_videoPlayer.IsPlaying())
        {
            return;
        }

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
            MoveSection(_currentSectionIndex - 1);
        }
        if (@event.IsActionPressed("right"))
        {
            MoveSection(_currentSectionIndex + 1);
        }
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            var mainMenuScreen = GD.Load<PackedScene>("res://scenes/screens/main_menu_screen.tscn");
            NGame.Instance.LoadScreen(mainMenuScreen);
        }
    }

    private void MoveSection(int index)
    {
        _currentSectionIndex = Mathf.Clamp(index, 0, _sections.Length - 1);
        var section = _sections[_currentSectionIndex];
        _videoPlayer.StreamPosition = section.Time;
        _videoPlayer.Play();
    }
}

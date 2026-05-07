using Godot;
using UniteBlocksRe.Logging;

public partial class NMainMenuScreen : Control
{
    public PackedScene _playScreen = GD.Load<PackedScene>("res://scenes/screens/play_screen.tscn");

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        Log.Info("Game Start");
        NGame.Instance.LoadScreen(_playScreen);
    }
}

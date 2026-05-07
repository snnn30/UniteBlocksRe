using Godot;

public partial class NMainMenuScreen : Control
{
    private void StartGame() { }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("rotate_left"))
        {
            var playScreen = GD.Load<PackedScene>("res://scenes/screens/play_screen.tscn");
            NGame.Instance.LoadScreen(playScreen);
        }
        if (@event.IsActionPressed("rotate_right"))
        {
            var gameDescriptionScreen = GD.Load<PackedScene>(
                "res://scenes/screens/game_description_screen.tscn"
            );
            NGame.Instance.LoadScreen(gameDescriptionScreen);
        }
    }
}

using Godot;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes;

public partial class NGame : Control
{
    private readonly PackedScene _mainMenuScreen = GD.Load<PackedScene>(
        "res://scenes/screens/main_menu_screen.tscn"
    );

    private Node _currentScreen = null!;

    public static NGame Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            Log.Warn("NBeatManagerインスタンスが複数ある");
            QueueFree();
            return;
        }
        Instance = this;

        LoadScreen(_mainMenuScreen);
    }

    public void LoadScreen(PackedScene screenPath)
    {
        _currentScreen?.QueueFree();
        _currentScreen = screenPath.Instantiate();
        AddChild(_currentScreen);
    }
}

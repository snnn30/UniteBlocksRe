using Godot;

namespace UniteBlocksRe.Nodes.PlayScreen;

public partial class NGameOverMessage : Control
{
    private Label _label;

    public void SetWinMessege()
    {
        _label.Text = "プレイヤーの勝利!!!";
    }

    public void SetLoseMessage()
    {
        _label.Text = "プレイヤーの敗北...";
    }

    public override void _Ready()
    {
        _label = GetNode<Label>("%Message");
    }
}

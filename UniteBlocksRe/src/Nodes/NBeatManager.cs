using System;
using Godot;
using UniteBlocksRe.Logging;

namespace UniteBlocksRe.Nodes;

public partial class NBeatManager : Node
{
    public static NBeatManager Instance { get; private set; }
    public int BPM { get; set; } = 180;

    // 4拍で刻むと仮定して、0, 1, 2, 3の順でビートカウントを通知する
    public event Action<int> OnBeat;
    public int BeatCount { get; private set; }
    private float _timer;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            Log.Warn("NBeatManagerインスタンスが複数ある");
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _Process(double delta)
    {
        var beatDuration = (float)60 / BPM;
        _timer += (float)delta;

        // 1フレームで複数ビート進む可能性があるため、ループで処理する
        while (_timer >= beatDuration)
        {
            _timer -= beatDuration;
            OnBeat?.Invoke(BeatCount);
            BeatCount = (BeatCount + 1) % 4;
        }
    }
}

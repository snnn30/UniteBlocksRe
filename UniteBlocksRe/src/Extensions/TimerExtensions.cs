using Godot;

namespace UniteBlocksRe.src.Extensions;

public static class TimerExtensions
{
    public static void ForceTimeout(this Timer timer)
    {
        timer.Stop();
        timer.EmitSignal(Timer.SignalName.Timeout);
    }
}

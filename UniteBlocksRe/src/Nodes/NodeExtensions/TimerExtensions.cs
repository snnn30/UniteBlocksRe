using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace UniteBlocksRe.src.Nodes.NodeExtensions;

public static class TimerExtensions
{
    public static void ForceTimeout(this Godot.Timer timer)
    {
        timer.Stop();
        timer.EmitSignal(Godot.Timer.SignalName.Timeout);
    }

    public static Task Delay(
        TimeSpan time,
        bool processAlways = false,
        CancellationToken ct = default
    )
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var timer = tree.CreateTimer(time.TotalSeconds, processAlways);
        ct.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource();
        CancellationTokenRegistration ctr = default;
        var unsubscribed = 0; // 0は購読中、1は購読解除済み

        timer.Timeout += OnFinished;

        if (ct.CanBeCanceled)
        {
            ctr = ct.Register(() =>
            {
                if (Interlocked.Exchange(ref unsubscribed, 1) == 0)
                {
                    timer.Timeout -= OnFinished;
                }
                tcs.TrySetCanceled(ct);
            });
        }

        return tcs.Task;

        void OnFinished()
        {
            if (Interlocked.Exchange(ref unsubscribed, 1) == 0)
            {
                timer.Timeout -= OnFinished;
            }
            ctr.Dispose();
            tcs.TrySetResult();
        }
    }
}

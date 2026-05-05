using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace UniteBlocksRe.Extensions;

public static class TweenExtensions
{
    public static void FastForwardToCompletion(this Tween t)
    {
        t.CustomStep(999.0);
    }

    public static Task WaitForFinished(this Tween tween, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource();
        CancellationTokenRegistration ctr = default;
        var unsubscribed = 0; // 0は購読中、1は購読解除済み

        tween.Finished += OnFinished;

        if (ct.CanBeCanceled)
        {
            ctr = ct.Register(() =>
            {
                // Interlocked.Exchangeは、unsubscribedを1に設定し、元の値を返す。
                // これにより、複数回の呼び出しで二重実行されないようにする。
                if (Interlocked.Exchange(ref unsubscribed, 1) == 0)
                {
                    tween.Finished -= OnFinished;
                }
                tcs.TrySetCanceled(ct);
            });
        }

        return tcs.Task;

        void OnFinished()
        {
            if (Interlocked.Exchange(ref unsubscribed, 1) == 0)
            {
                tween.Finished -= OnFinished;
            }
            ctr.Dispose();
            tcs.TrySetResult();
        }
    }
}

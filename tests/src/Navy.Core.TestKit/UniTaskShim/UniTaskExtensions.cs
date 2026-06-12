// Extension methods for the UniTask shim.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Threading.Tasks
{
    public static class UniTaskExtensions
    {
        // ─── Task → UniTask ───────────────────────────────────────────────────

        public static UniTask AsUniTask(this Task task) => new UniTask(task);

        public static UniTask<T> AsUniTask<T>(this Task<T> task) => new UniTask<T>(task);

        // ─── Fire-and-forget ──────────────────────────────────────────────────

        public static void Forget(this UniTask task)
        {
            task.AsTask().ContinueWith(
                t => Console.WriteLine($"[UniTask.Forget] Exception: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void Forget<T>(this UniTask<T> task)
        {
            task.AsTask().ContinueWith(
                t => Console.WriteLine($"[UniTask.Forget] Exception: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        // ─── AttachExternalCancellation ───────────────────────────────────────

        public static UniTask AttachExternalCancellation(this UniTask task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled) return task;
            return new UniTask(task.AsTask().WaitAsync(ct));
        }

        public static UniTask<T> AttachExternalCancellation<T>(this UniTask<T> task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled) return task;
            return new UniTask<T>(task.AsTask().WaitAsync(ct));
        }
    }
}

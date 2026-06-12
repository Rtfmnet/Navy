// UniTask shim for PC1 test compilation.
// Provides a minimal, source-compatible stub of Cysharp.Threading.Tasks
// so that Navy.Core.Contracts.ISessionService can compile without Unity.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Threading.Tasks
{
    // ─── UniTask (non-generic) ────────────────────────────────────────────────

    [AsyncMethodBuilder(typeof(UniTaskMethodBuilder))]
    public readonly struct UniTask
    {
        private readonly Task _task;

        public UniTask(Task task) { _task = task ?? Task.CompletedTask; }

        public static UniTask CompletedTask => new UniTask(Task.CompletedTask);

        public Task AsTask() => _task ?? Task.CompletedTask;

        public static implicit operator UniTask(Task t) => new UniTask(t);
        public static implicit operator Task(UniTask ut) => ut.AsTask();

        public UniTaskAwaiter GetAwaiter() => new UniTaskAwaiter(_task ?? Task.CompletedTask);

        public static UniTask Delay(int millisecondsDelay, CancellationToken cancellationToken = default)
            => new UniTask(Task.Delay(millisecondsDelay, cancellationToken));

        public static UniTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
            => new UniTask(Task.Delay(delay, cancellationToken));

        public static UniTask FromException(Exception e)
            => new UniTask(Task.FromException(e));

        public static UniTask<T> FromResult<T>(T value)
            => new UniTask<T>(Task.FromResult(value));

        public static UniTask WhenAll(params UniTask[] tasks)
        {
            var all = new Task[tasks.Length];
            for (int i = 0; i < tasks.Length; i++) all[i] = tasks[i].AsTask();
            return new UniTask(Task.WhenAll(all));
        }
    }

    // ─── UniTask<T> (generic) ─────────────────────────────────────────────────

    [AsyncMethodBuilder(typeof(UniTaskMethodBuilder<>))]
    public readonly struct UniTask<T>
    {
        private readonly Task<T> _task;

        public UniTask(Task<T> task) { _task = task; }

        public Task<T> AsTask() => _task;

        public static implicit operator UniTask<T>(Task<T> t) => new UniTask<T>(t);
        public static implicit operator Task<T>(UniTask<T> ut) => ut.AsTask();

        public UniTaskAwaiter<T> GetAwaiter() => new UniTaskAwaiter<T>(_task);
    }

    // ─── UniTaskVoid ──────────────────────────────────────────────────────────

    [AsyncMethodBuilder(typeof(UniTaskVoidMethodBuilder))]
    public readonly struct UniTaskVoid
    {
        public void Forget() { }
    }

    // ─── Awaiters ─────────────────────────────────────────────────────────────

    public struct UniTaskAwaiter : INotifyCompletion
    {
        private readonly TaskAwaiter _inner;
        internal UniTaskAwaiter(Task task) { _inner = task.GetAwaiter(); }
        public bool IsCompleted => _inner.IsCompleted;
        public void GetResult() => _inner.GetResult();
        public void OnCompleted(Action continuation) => _inner.OnCompleted(continuation);
    }

    public struct UniTaskAwaiter<T> : INotifyCompletion
    {
        private readonly TaskAwaiter<T> _inner;
        internal UniTaskAwaiter(Task<T> task) { _inner = task.GetAwaiter(); }
        public bool IsCompleted => _inner.IsCompleted;
        public T GetResult() => _inner.GetResult();
        public void OnCompleted(Action continuation) => _inner.OnCompleted(continuation);
    }

    // ─── Async method builders ────────────────────────────────────────────────

    public class UniTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder _inner = AsyncTaskMethodBuilder.Create();

        public static UniTaskMethodBuilder Create() => new UniTaskMethodBuilder();
        public UniTask Task => new UniTask(_inner.Task);

        public void Start<TStateMachine>(ref TStateMachine machine)
            where TStateMachine : IAsyncStateMachine => _inner.Start(ref machine);

        public void SetStateMachine(IAsyncStateMachine machine) => _inner.SetStateMachine(machine);
        public void SetResult() => _inner.SetResult();
        public void SetException(Exception e) => _inner.SetException(e);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _inner.AwaitOnCompleted(ref awaiter, ref machine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _inner.AwaitUnsafeOnCompleted(ref awaiter, ref machine);
    }

    public class UniTaskMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _inner = AsyncTaskMethodBuilder<T>.Create();

        public static UniTaskMethodBuilder<T> Create() => new UniTaskMethodBuilder<T>();
        public UniTask<T> Task => new UniTask<T>(_inner.Task);

        public void Start<TStateMachine>(ref TStateMachine machine)
            where TStateMachine : IAsyncStateMachine => _inner.Start(ref machine);

        public void SetStateMachine(IAsyncStateMachine machine) => _inner.SetStateMachine(machine);
        public void SetResult(T result) => _inner.SetResult(result);
        public void SetException(Exception e) => _inner.SetException(e);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _inner.AwaitOnCompleted(ref awaiter, ref machine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _inner.AwaitUnsafeOnCompleted(ref awaiter, ref machine);
    }

    public class UniTaskVoidMethodBuilder
    {
        public static UniTaskVoidMethodBuilder Create() => new UniTaskVoidMethodBuilder();
        public UniTaskVoid Task => default;

        public void Start<TStateMachine>(ref TStateMachine machine)
            where TStateMachine : IAsyncStateMachine => machine.MoveNext();

        public void SetStateMachine(IAsyncStateMachine machine) { }
        public void SetResult() { }
        public void SetException(Exception e) => Console.WriteLine($"[UniTaskVoid] Exception: {e}");

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(machine.MoveNext);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine machine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(machine.MoveNext);
    }
}

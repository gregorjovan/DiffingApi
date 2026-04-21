using DiffingApi.Advanced.Application.Abstractions;
using System.Collections.Concurrent;

namespace DiffingApi.Advanced.Application.Services;

public sealed class DiffLockProvider : IDiffLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async ValueTask<IAsyncDisposable> AcquireAsync(string id, CancellationToken ct = default)
    {
        var gate = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        return new LockLease(gate);
    }

    private sealed class LockLease : IAsyncDisposable
    {
        private readonly SemaphoreSlim _gate;
        private int _disposed;

        public LockLease(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}

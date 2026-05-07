using System.Collections.Concurrent;
using System.Threading.Channels;
using DiffingApi.Advanced.Application.Abstractions;

namespace DiffingApi.Advanced.Application.Services;

public sealed class DiffJobQueue : IDiffJobQueue
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly ConcurrentDictionary<string, DiffJobState> _states = new();

    public async ValueTask QueueAsync(string id, CancellationToken ct = default)
    {
        var state = _states.GetOrAdd(id, _ => new DiffJobState());
        var shouldQueue = false;

        lock (state)
        {
            state.RequestedGeneration++;

            if (state.Status != DiffProcessingStatuses.Processing && !state.IsQueued)
            {
                state.Status = DiffProcessingStatuses.Pending;
                state.IsQueued = true;
                shouldQueue = true;
            }
        }

        if (shouldQueue)
        {
            await _queue.Writer.WriteAsync(id, ct);
        }
    }

    public async ValueTask EnsureQueuedAsync(string id, CancellationToken ct = default)
    {
        var state = _states.GetOrAdd(id, _ => new DiffJobState());
        var shouldQueue = false;

        lock (state)
        {
            if (state.Status != DiffProcessingStatuses.Processing && !state.IsQueued)
            {
                state.Status = DiffProcessingStatuses.Pending;
                state.IsQueued = true;
                shouldQueue = true;
            }
        }

        if (shouldQueue)
        {
            await _queue.Writer.WriteAsync(id, ct);
        }
    }

    public ValueTask<string> DequeueAsync(CancellationToken ct = default)
    {
        return _queue.Reader.ReadAsync(ct);
    }

    public bool TryStart(string id, out long generation)
    {
        generation = 0;

        if (!_states.TryGetValue(id, out var state))
        {
            return false;
        }

        lock (state)
        {
            state.IsQueued = false;
            state.Status = DiffProcessingStatuses.Processing;
            state.ProcessingGeneration = state.RequestedGeneration;
            generation = state.ProcessingGeneration;
            return true;
        }
    }

    public async ValueTask CompleteAsync(
        string id,
        long generation,
        CancellationToken ct = default)
    {
        if (!_states.TryGetValue(id, out var state))
        {
            return;
        }

        var shouldQueue = false;

        lock (state)
        {
            if (state.RequestedGeneration > generation)
            {
                state.Status = DiffProcessingStatuses.Pending;

                if (!state.IsQueued)
                {
                    state.IsQueued = true;
                    shouldQueue = true;
                }
            }
            else
            {
                state.Status = DiffProcessingStatuses.Completed;
            }
        }

        if (shouldQueue)
        {
            await _queue.Writer.WriteAsync(id, ct);
        }
    }

    public async ValueTask FailAsync(
        string id,
        long generation,
        CancellationToken ct = default)
    {
        if (!_states.TryGetValue(id, out var state))
        {
            return;
        }

        var shouldQueue = false;

        lock (state)
        {
            if (state.RequestedGeneration > generation)
            {
                state.Status = DiffProcessingStatuses.Pending;

                if (!state.IsQueued)
                {
                    state.IsQueued = true;
                    shouldQueue = true;
                }
            }
            else
            {
                state.Status = DiffProcessingStatuses.Failed;
            }
        }

        if (shouldQueue)
        {
            await _queue.Writer.WriteAsync(id, ct);
        }
    }

    private sealed class DiffJobState
    {
        public string Status { get; set; } = DiffProcessingStatuses.Pending;
        public long RequestedGeneration { get; set; }
        public long ProcessingGeneration { get; set; }
        public bool IsQueued { get; set; }
    }
}

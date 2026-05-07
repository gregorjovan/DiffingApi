namespace DiffingApi.Advanced.Application.Abstractions;

public interface IDiffJobQueue
{
    ValueTask QueueAsync(string id, CancellationToken ct = default);
    ValueTask EnsureQueuedAsync(string id, CancellationToken ct = default);
    ValueTask<string> DequeueAsync(CancellationToken ct = default);
    bool TryStart(string id, out long generation);
    ValueTask CompleteAsync(string id, long generation, CancellationToken ct = default);
    ValueTask FailAsync(string id, long generation, CancellationToken ct = default);
}

using DiffingApi.Advanced.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace DiffingApi.Advanced.Application.Services;

public sealed class DiffService : IDiffService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    private readonly IDiffPairRepository _repository;
    private readonly IDiffLockProvider _lockProvider;
    private readonly IMemoryCache _cache;
    private readonly IDiffJobQueue _jobQueue;

    public DiffService(
        IDiffPairRepository repository,
        IDiffLockProvider lockProvider,
        IMemoryCache cache,
        IDiffJobQueue jobQueue)
    {
        _repository = repository;
        _lockProvider = lockProvider;
        _cache = cache;
        _jobQueue = jobQueue;
    }

    public async Task SaveLeftAsync(string id, byte[] data, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            await _repository.UpsertLeftAsync(id, data, ct);
            _cache.Remove(GetCacheKey(id));
            await QueueDiffIfReadyAsync(id, ct);
        }
    }

    public async Task SaveRightAsync(string id, byte[] data, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            await _repository.UpsertRightAsync(id, data, ct);
            _cache.Remove(GetCacheKey(id));
            await QueueDiffIfReadyAsync(id, ct);
        }
    }

    public async Task<DiffResult?> GetDiffAsync(string id, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            DiffPairEntity? pair;

            if (!_cache.TryGetValue(GetCacheKey(id), out pair))
            {
                pair = await _repository.GetAsync(id, ct);

                if (pair is null)
                {
                    return null;
                }

                _cache.Set(GetCacheKey(id), pair, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                });
            }

            if (pair is null || pair.Left is null || pair.Right is null)
            {
                return null;
            }

            return DiffResultFactory.Create(pair.Left, pair.Right);
        }
    }

    public async Task<DiffStatusResult?> GetDiffStatusAsync(string id, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            var pair = await _repository.GetAsync(id, ct);

            if (pair is null || pair.Left is null || pair.Right is null)
            {
                return null;
            }

            if (pair.ProcessingStatus is null)
            {
                await _repository.MarkPendingAsync(id, ct);
                await _jobQueue.QueueAsync(id, ct);
                return new DiffStatusResult(DiffProcessingStatuses.Pending);
            }

            if (pair.ProcessingStatus is DiffProcessingStatuses.Pending or DiffProcessingStatuses.Processing)
            {
                await _jobQueue.EnsureQueuedAsync(id, ct);
            }

            return CreateStatusResult(pair);
        }
    }

    private async Task QueueDiffIfReadyAsync(string id, CancellationToken ct)
    {
        var pair = await _repository.GetAsync(id, ct);

        if (pair?.Left is not null && pair.Right is not null)
        {
            await _repository.MarkPendingAsync(id, ct);
            await _jobQueue.QueueAsync(id, ct);
        }
    }

    private static DiffStatusResult CreateStatusResult(DiffPairEntity pair)
    {
        if (pair.ProcessingStatus == DiffProcessingStatuses.Completed)
        {
            var diffs = string.IsNullOrWhiteSpace(pair.DiffsJson)
                ? null
                : JsonSerializer.Deserialize<DiffRangeResult[]>(pair.DiffsJson);

            return new DiffStatusResult(
                pair.ProcessingStatus,
                pair.DiffResultType,
                diffs);
        }

        if (pair.ProcessingStatus == DiffProcessingStatuses.Failed)
        {
            return new DiffStatusResult(pair.ProcessingStatus, Reason: pair.FailureReason);
        }

        return new DiffStatusResult(pair.ProcessingStatus ?? DiffProcessingStatuses.Pending);
    }

    private static string GetCacheKey(string id) => $"diffpair:{id}";
}

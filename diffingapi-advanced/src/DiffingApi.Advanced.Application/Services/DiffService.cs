using DiffingApi.Advanced.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace DiffingApi.Advanced.Application.Services;

public sealed class DiffService : IDiffService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    private readonly IDiffPairRepository _repository;
    private readonly IDiffLockProvider _lockProvider;
    private readonly IMemoryCache _cache;

    public DiffService(
        IDiffPairRepository repository,
        IDiffLockProvider lockProvider,
        IMemoryCache cache)
    {
        _repository = repository;
        _lockProvider = lockProvider;
        _cache = cache;
    }

    public async Task SaveLeftAsync(string id, byte[] data, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            await _repository.UpsertLeftAsync(id, data, ct);
            _cache.Remove(GetCacheKey(id));
        }
    }

    public async Task SaveRightAsync(string id, byte[] data, CancellationToken ct = default)
    {
        await using (await _lockProvider.AcquireAsync(id, ct))
        {
            await _repository.UpsertRightAsync(id, data, ct);
            _cache.Remove(GetCacheKey(id));
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

            if (pair.Left.Length != pair.Right.Length)
            {
                return new DiffResult("SizeDoNotMatch");
            }

            if (pair.Left.SequenceEqual(pair.Right))
            {
                return new DiffResult("Equals");
            }

            var diffs = DiffCalculator.FindDiffs(pair.Left, pair.Right)
                .Select(diff => new DiffRangeResult(diff.Offset, diff.Length))
                .ToArray();

            return new DiffResult("ContentDoNotMatch", diffs);
        }
    }

    private static string GetCacheKey(string id) => $"diffpair:{id}";
}

using System.Collections.Concurrent;
using DiffingApi.Advanced.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace DiffingApi.UnitTests;

public sealed class DiffServiceTests
{
    [Fact]
    public async Task GetDiffAsync_WhenOnlyLeftExists_ReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });

        var result = await service.GetDiffAsync("id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDiffAsync_WhenPayloadSizesDiffer_ReturnsSizeDoNotMatch()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });
        await service.SaveRightAsync("id", new byte[] { 1, 2 });

        var result = await service.GetDiffAsync("id");

        Assert.NotNull(result);
        Assert.Equal("SizeDoNotMatch", result!.DiffResultType);
        Assert.Null(result.Diffs);
    }

    [Fact]
    public async Task GetDiffAsync_WhenPayloadsAreEqual_ReturnsEquals()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });
        await service.SaveRightAsync("id", new byte[] { 1, 2, 3 });

        var result = await service.GetDiffAsync("id");

        Assert.NotNull(result);
        Assert.Equal("Equals", result!.DiffResultType);
        Assert.Null(result.Diffs);
    }

    [Fact]
    public async Task GetDiffAsync_WhenPayloadsDifferWithSameLength_ReturnsDiffRanges()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3, 4 });
        await service.SaveRightAsync("id", new byte[] { 1, 9, 3, 8 });

        var result = await service.GetDiffAsync("id");

        Assert.NotNull(result);
        Assert.Equal("ContentDoNotMatch", result!.DiffResultType);
        Assert.Equal(
            new[]
            {
                new DiffRangeResult(1, 1),
                new DiffRangeResult(3, 1)
            },
            result.Diffs);
    }

    private static DiffService CreateService(
        IDiffPairRepository repository,
        IMemoryCache cache)
    {
        return new DiffService(repository, new DiffLockProvider(), cache);
    }

    private sealed class InMemoryDiffPairRepository : IDiffPairRepository
    {
        private readonly ConcurrentDictionary<string, DiffPairEntity> _entries = new();

        public Task<DiffPairEntity?> GetAsync(string id, CancellationToken ct = default)
        {
            _entries.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task UpsertLeftAsync(string id, byte[] data, CancellationToken ct = default)
        {
            _entries.AddOrUpdate(
                id,
                _ => new DiffPairEntity
                {
                    Id = id,
                    Left = data
                },
                (_, existing) => new DiffPairEntity
                {
                    Id = existing.Id,
                    Left = data,
                    Right = existing.Right
                });

            return Task.CompletedTask;
        }

        public Task UpsertRightAsync(string id, byte[] data, CancellationToken ct = default)
        {
            _entries.AddOrUpdate(
                id,
                _ => new DiffPairEntity
                {
                    Id = id,
                    Right = data
                },
                (_, existing) => new DiffPairEntity
                {
                    Id = existing.Id,
                    Left = existing.Left,
                    Right = data
                });

            return Task.CompletedTask;
        }
    }
}

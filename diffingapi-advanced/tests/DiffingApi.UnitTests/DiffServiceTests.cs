using System.Collections.Concurrent;
using System.Text.Json;
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

    [Fact]
    public async Task GetDiffStatusAsync_WhenOnlyLeftExists_ReturnsNull()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });

        var result = await service.GetDiffStatusAsync("id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDiffStatusAsync_WhenBothSidesExist_ReturnsPending()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });
        await service.SaveRightAsync("id", new byte[] { 1, 2, 3 });

        var result = await service.GetDiffStatusAsync("id");

        Assert.NotNull(result);
        Assert.Equal("Pending", result!.Status);
    }

    [Fact]
    public async Task GetDiffStatusAsync_WhenPersistedResultExists_ReturnsCompletedResult()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new InMemoryDiffPairRepository();
        var service = CreateService(repository, cache);

        await service.SaveLeftAsync("id", new byte[] { 1, 2, 3 });
        await service.SaveRightAsync("id", new byte[] { 1, 9, 3 });
        await repository.SaveDiffResultAsync(
            "id",
            new DiffResult(
                "ContentDoNotMatch",
                new[] { new DiffRangeResult(1, 1) }));

        var result = await service.GetDiffStatusAsync("id");

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.Status);
        Assert.Equal("ContentDoNotMatch", result.DiffResultType);
        Assert.Equal(new[] { new DiffRangeResult(1, 1) }, result.Diffs);
    }

    private static DiffService CreateService(
        IDiffPairRepository repository,
        IMemoryCache cache)
    {
        return new DiffService(repository, new DiffLockProvider(), cache, new DiffJobQueue());
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

        public Task MarkPendingAsync(string id, CancellationToken ct = default)
        {
            Update(id, entity =>
            {
                entity.ProcessingStatus = "Pending";
                entity.DiffResultType = null;
                entity.DiffsJson = null;
                entity.FailureReason = null;
            });

            return Task.CompletedTask;
        }

        public Task MarkProcessingAsync(string id, CancellationToken ct = default)
        {
            Update(id, entity =>
            {
                entity.ProcessingStatus = "Processing";
                entity.FailureReason = null;
            });

            return Task.CompletedTask;
        }

        public Task SaveDiffResultAsync(string id, DiffResult result, CancellationToken ct = default)
        {
            Update(id, entity =>
            {
                entity.ProcessingStatus = "Completed";
                entity.DiffResultType = result.DiffResultType;
                entity.DiffsJson = result.Diffs is null ? null : JsonSerializer.Serialize(result.Diffs);
                entity.FailureReason = null;
            });

            return Task.CompletedTask;
        }

        public Task SaveDiffFailureAsync(string id, string reason, CancellationToken ct = default)
        {
            Update(id, entity =>
            {
                entity.ProcessingStatus = "Failed";
                entity.DiffResultType = null;
                entity.DiffsJson = null;
                entity.FailureReason = reason;
            });

            return Task.CompletedTask;
        }

        private void Update(string id, Action<DiffPairEntity> update)
        {
            _entries.AddOrUpdate(
                id,
                _ =>
                {
                    var entity = new DiffPairEntity { Id = id };
                    update(entity);
                    return entity;
                },
                (_, existing) =>
                {
                    var entity = new DiffPairEntity
                    {
                        Id = existing.Id,
                        Left = existing.Left,
                        Right = existing.Right,
                        ProcessingStatus = existing.ProcessingStatus,
                        DiffResultType = existing.DiffResultType,
                        DiffsJson = existing.DiffsJson,
                        FailureReason = existing.FailureReason
                    };

                    update(entity);
                    return entity;
                });
        }
    }
}

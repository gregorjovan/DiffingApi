using System.Text.Json;
using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Application.Services;
using DiffingApi.Advanced.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiffingApi.Advanced.Infrastructure.Repositories;

public sealed class SqliteDiffPairRepository : IDiffPairRepository
{
    private readonly DiffDbContext _dbContext;

    public SqliteDiffPairRepository(DiffDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DiffPairEntity?> GetAsync(string id, CancellationToken ct = default)
    {
        return await _dbContext.DiffPairs
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task UpsertLeftAsync(string id, byte[] data, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            entity = new DiffPairEntity
            {
                Id = id,
                Left = data
            };

            _dbContext.DiffPairs.Add(entity);
        }
        else
        {
            entity.Left = data;
            ClearDiffState(entity);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpsertRightAsync(string id, byte[] data, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            entity = new DiffPairEntity
            {
                Id = id,
                Right = data
            };

            _dbContext.DiffPairs.Add(entity);
        }
        else
        {
            entity.Right = data;
            ClearDiffState(entity);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkPendingAsync(string id, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleAsync(x => x.Id == id, ct);

        entity.ProcessingStatus = DiffProcessingStatuses.Pending;
        entity.DiffResultType = null;
        entity.DiffsJson = null;
        entity.FailureReason = null;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkProcessingAsync(string id, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleAsync(x => x.Id == id, ct);

        entity.ProcessingStatus = DiffProcessingStatuses.Processing;
        entity.FailureReason = null;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveDiffResultAsync(string id, DiffResult result, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleAsync(x => x.Id == id, ct);

        entity.ProcessingStatus = DiffProcessingStatuses.Completed;
        entity.DiffResultType = result.DiffResultType;
        entity.DiffsJson = result.Diffs is null ? null : JsonSerializer.Serialize(result.Diffs);
        entity.FailureReason = null;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveDiffFailureAsync(string id, string reason, CancellationToken ct = default)
    {
        var entity = await _dbContext.DiffPairs
            .SingleAsync(x => x.Id == id, ct);

        entity.ProcessingStatus = DiffProcessingStatuses.Failed;
        entity.DiffResultType = null;
        entity.DiffsJson = null;
        entity.FailureReason = reason;

        await _dbContext.SaveChangesAsync(ct);
    }

    private static void ClearDiffState(DiffPairEntity entity)
    {
        entity.ProcessingStatus = null;
        entity.DiffResultType = null;
        entity.DiffsJson = null;
        entity.FailureReason = null;
    }
}

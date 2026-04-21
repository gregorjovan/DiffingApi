using DiffingApi.Advanced.Application.Abstractions;
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
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
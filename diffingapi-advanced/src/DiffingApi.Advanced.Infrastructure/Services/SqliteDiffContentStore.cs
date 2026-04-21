using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Domain.Models;
using DiffingApi.Advanced.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiffingApi.Advanced.Infrastructure.Services;

public sealed class SqliteDiffContentStore(IDbContextFactory<DiffingAdvancedDbContext> dbContextFactory) : IDiffContentStore
{
    public void SetLeft(string id, byte[] data)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var entity = dbContext.DiffEntries.SingleOrDefault(entry => entry.Id == id);

        if (entity is null)
        {
            dbContext.DiffEntries.Add(new DiffEntryEntity
            {
                Id = id,
                Left = data
            });
        }
        else
        {
            entity.Left = data;
        }

        dbContext.SaveChanges();
    }

    public void SetRight(string id, byte[] data)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var entity = dbContext.DiffEntries.SingleOrDefault(entry => entry.Id == id);

        if (entity is null)
        {
            dbContext.DiffEntries.Add(new DiffEntryEntity
            {
                Id = id,
                Right = data
            });
        }
        else
        {
            entity.Right = data;
        }

        dbContext.SaveChanges();
    }

    public bool TryGet(string id, out DiffEntry? entry)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var entity = dbContext.DiffEntries
            .AsNoTracking()
            .SingleOrDefault(candidate => candidate.Id == id);

        if (entity is null)
        {
            entry = null;
            return false;
        }

        entry = new DiffEntry(entity.Left, entity.Right);
        return true;
    }
}

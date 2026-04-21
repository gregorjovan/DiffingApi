using Microsoft.EntityFrameworkCore;

namespace DiffingApi.Advanced.Infrastructure.Persistence;

public sealed class DiffingAdvancedDbContext(DbContextOptions<DiffingAdvancedDbContext> options) : DbContext(options)
{
    public DbSet<DiffEntryEntity> DiffEntries => Set<DiffEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiffEntryEntity>(entity =>
        {
            entity.ToTable("DiffEntries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).IsRequired();
        });
    }
}

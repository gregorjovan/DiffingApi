using DiffingApi.Advanced.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DiffingApi.Advanced.Infrastructure.Persistence;

public sealed class DiffDbContext : DbContext
{
    public DiffDbContext(DbContextOptions<DiffDbContext> options)
        : base(options)
    {
    }

    public DbSet<DiffPairEntity> DiffPairs => Set<DiffPairEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiffPairEntity>(entity =>
        {
            entity.ToTable("DiffPairs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .IsRequired();

            entity.Property(x => x.Left)
                .HasColumnType("BLOB");

            entity.Property(x => x.Right)
                .HasColumnType("BLOB");

            entity.Property(x => x.ProcessingStatus)
                .HasMaxLength(32);

            entity.Property(x => x.DiffResultType)
                .HasMaxLength(32);

            entity.Property(x => x.DiffsJson)
                .HasColumnType("TEXT");

            entity.Property(x => x.FailureReason)
                .HasColumnType("TEXT");
        });
    }
}

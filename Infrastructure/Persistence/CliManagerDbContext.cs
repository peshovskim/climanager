using CliManager.Domain.Drive;
using Microsoft.EntityFrameworkCore;

namespace CliManager.Infrastructure.Persistence;

public sealed class CliManagerDbContext(DbContextOptions<CliManagerDbContext> options) : DbContext(options)
{
    public DbSet<SyncEntry> SyncEntries => Set<SyncEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CliManagerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

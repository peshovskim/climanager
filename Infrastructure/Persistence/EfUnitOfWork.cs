using CliManager.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CliManager.Infrastructure.Persistence;

public sealed class EfUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
    where TDbContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

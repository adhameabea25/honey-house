using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();

    public IGenericRepository<T> Repository<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repo))
        {
            repo = new GenericRepository<T>(db);
            _repositories[typeof(T)] = repo;
        }
        return (IGenericRepository<T>)repo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The data was modified by another request.", ex);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        Func<TResult, bool>? shouldCommit = null,
        CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null || !db.Database.IsRelational())
            return await operation(ct);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async token =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(token);
            var result = await operation(token);
            if (shouldCommit is null || shouldCommit(result))
                await tx.CommitAsync(token);
            else
            {
                await tx.RollbackAsync(token);
                db.ChangeTracker.Clear();
            }
            return result;
        }, ct);
    }
}

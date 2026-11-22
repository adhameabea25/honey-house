namespace HoneyHouse.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs the operation inside a database transaction, committing on success and rolling back on exception or failed result.</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, Func<TResult, bool>? shouldCommit = null, CancellationToken ct = default);
}

/// <summary>Raised when an optimistic concurrency check fails (e.g. two orders racing for the last stock).</summary>
public sealed class ConcurrencyConflictException(string message, Exception? inner = null) : Exception(message, inner);

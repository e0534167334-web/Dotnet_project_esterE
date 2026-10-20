namespace Core.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    // טעינה עם tracking (לעדכון)
    Task<T?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Remove(T entity);

    /// <exception cref="Core.Exceptions.ConcurrencyConflictException"/>
    Task SaveChangesAsync(CancellationToken ct);
}

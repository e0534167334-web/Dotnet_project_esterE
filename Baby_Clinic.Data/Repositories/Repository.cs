using Core.Exceptions;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> Set;

    public Repository(AppDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct) =>
        await Set.FindAsync(new object[] { id }, ct);

    public async Task AddAsync(T entity, CancellationToken ct) =>
        await Set.AddAsync(entity, ct);

    public void Remove(T entity) => Set.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await Context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // ===== שלב 7 – תחרות על משאב (3/4): תרגום החריגה =====
            // ה-Service לא מכיר EF, ולכן מקבל חריגה משלנו (מ-Core)
            throw new ConcurrencyConflictException("The record was modified by another request.", ex);
        }
    }
}

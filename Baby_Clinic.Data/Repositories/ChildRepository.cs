using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data.Repositories;

public class ChildRepository : Repository<Child>, IChildRepository
{
    public ChildRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Child>> GetByUserIdAsync(int userId, CancellationToken ct) =>
        await Set.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
}

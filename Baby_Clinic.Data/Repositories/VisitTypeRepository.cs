using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data.Repositories;

public class VisitTypeRepository : Repository<VisitType>, IVisitTypeRepository
{
    public VisitTypeRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<VisitType>> GetAllAsync(CancellationToken ct) =>
        await Set.AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

    // עם tracking: הסוגים משויכים למשבצת בקשר many-to-many ונשמרים איתה
    public async Task<IReadOnlyList<VisitType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        return await Set.Where(v => idList.Contains(v.Id)).ToListAsync(ct);
    }
}

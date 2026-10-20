using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data.Repositories;

public class SlotRepository : Repository<Slot>, ISlotRepository
{
    public SlotRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Slot> Items, int TotalCount)> SearchAsync(SlotSearchParams search, CancellationToken ct)
    {
        IQueryable<Slot> query = Set.AsNoTracking();

        if (search.From.HasValue)
        {
            var from = ToUtc(search.From.Value);
            query = query.Where(s => s.StartTimeUtc >= from);
        }

        if (search.To.HasValue)
        {
            var to = ToUtc(search.To.Value);
            query = query.Where(s => s.StartTimeUtc <= to);
        }

        if (search.VisitTypeId.HasValue)
        {
            var visitTypeId = search.VisitTypeId.Value;
            query = query.Where(s => s.VisitTypes.Any(v => v.Id == visitTypeId));
        }

        if (search.AvailableOnly)
            query = query.Where(s => s.Status == SlotStatus.Available);

        var totalCount = await query.CountAsync(ct);

        // Skip ו-Take בתוך השאילתה - החיתוך נעשה בבסיס הנתונים ולא בזיכרון
        var items = await query
            .OrderBy(s => s.StartTimeUtc)
            .ThenBy(s => s.Id)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Include(s => s.VisitTypes)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Slot?> GetWithVisitTypesAsync(int id, CancellationToken ct) =>
        Set.AsNoTracking()
            .Include(s => s.VisitTypes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Slot?> GetWithVisitTypesForUpdateAsync(int id, CancellationToken ct) =>
        Set.Include(s => s.VisitTypes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> HasAppointmentsAsync(int slotId, CancellationToken ct) =>
        Context.Appointments.AnyAsync(a => a.SlotId == slotId, ct);

    // עמודת timestamptz דורשת DateTime מסוג Utc; ערך מה-query string מגיע לרוב כ-Unspecified
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data.Repositories;

public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }

    public Task<Appointment?> GetWithDetailsAsync(int id, CancellationToken ct) =>
        Set.Include(a => a.Child)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    // Include ו-ThenInclude בשאילתה אחת, בלי N+1
    public async Task<IReadOnlyList<Appointment>> GetListAsync(int? userId, CancellationToken ct)
    {
        IQueryable<Appointment> query = Set.AsNoTracking()
            .Include(a => a.Child)
            .Include(a => a.Slot)
                .ThenInclude(s => s!.VisitTypes);

        if (userId.HasValue)
        {
            var parentId = userId.Value;
            query = query.Where(a => a.Child!.UserId == parentId);
        }

        return await query
            .OrderBy(a => a.Slot!.StartTimeUtc)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public Task<int> CountActiveByChildAsync(int childId, CancellationToken ct) =>
        Set.CountAsync(a => a.ChildId == childId && a.Status == AppointmentStatus.Active, ct);
}

using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    // עם tracking, כולל Child ו-Slot (לביטול)
    Task<Appointment?> GetWithDetailsAsync(int id, CancellationToken ct);

    // קריאה בלבד, כולל Child ו-Slot; userId == null => כל התורים
    Task<IReadOnlyList<Appointment>> GetListAsync(int? userId, CancellationToken ct);

    Task<int> CountActiveByChildAsync(int childId, CancellationToken ct);
}

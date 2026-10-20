using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface ISlotRepository : IRepository<Slot>
{
    // קריאה בלבד (AsNoTracking), pagination בתוך השאילתה
    Task<(IReadOnlyList<Slot> Items, int TotalCount)> SearchAsync(SlotSearchParams search, CancellationToken ct);

    // קריאה בלבד, כולל VisitTypes
    Task<Slot?> GetWithVisitTypesAsync(int id, CancellationToken ct);

    // עם tracking, כולל VisitTypes (לעדכון)
    Task<Slot?> GetWithVisitTypesForUpdateAsync(int id, CancellationToken ct);

    Task<bool> HasAppointmentsAsync(int slotId, CancellationToken ct);
}

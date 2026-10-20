using Core.Common;
using Core.DTOs;
using Core.Enums;

namespace Core.Interfaces.Services;

public interface IAppointmentService
{
    Task<Result<AppointmentResponse>> BookAsync(int parentId, BookAppointmentRequest request, CancellationToken ct);
    Task<Result> CancelAsync(int appointmentId, int userId, Role role, CancellationToken ct);

    // Parent: התורים שלו בלבד. Admin: הכול
    Task<Result<List<AppointmentResponse>>> GetAsync(int userId, Role role, CancellationToken ct);
}

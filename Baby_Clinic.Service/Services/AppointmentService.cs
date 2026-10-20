using AutoMapper;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Baby_Clinic.Service.Services;

public class AppointmentService : IAppointmentService
{
    public const int MaxActiveAppointmentsPerChild = 2;

    private readonly IAppointmentRepository _appointments;
    private readonly ISlotRepository _slots;
    private readonly IChildRepository _children;
    private readonly IMapper _mapper;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IAppointmentRepository appointments,
        ISlotRepository slots,
        IChildRepository children,
        IMapper mapper,
        ILogger<AppointmentService> logger)
    {
        _appointments = appointments;
        _slots = slots;
        _children = children;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AppointmentResponse>> BookAsync(int parentId, BookAppointmentRequest request, CancellationToken ct)
    {
        // עם tracking: ה-Version (xmin) שנטען כאן נבדק בזמן השמירה
        var slot = await _slots.GetByIdAsync(request.SlotId, ct);
        if (slot is null)
            return Result<AppointmentResponse>.Fail(ResultStatus.NotFound, $"Slot {request.SlotId} was not found.");

        var child = await _children.GetByIdAsync(request.ChildId, ct);
        if (child is null)
            return Result<AppointmentResponse>.Fail(ResultStatus.NotFound, $"Child {request.ChildId} was not found.");

        if (child.UserId != parentId)
            return Result<AppointmentResponse>.Fail(ResultStatus.Forbidden, "You can book appointments only for your own children.");

        if (slot.StartTimeUtc <= DateTime.UtcNow)
            return Result<AppointmentResponse>.Fail(ResultStatus.Validation, "Cannot book a slot in the past.");

        if (slot.Status != SlotStatus.Available)
        {
            _logger.LogWarning("Booking conflict: slot {SlotId} is already booked (child {ChildId}).", slot.Id, child.Id);
            return Result<AppointmentResponse>.Fail(ResultStatus.Conflict, "This slot is already booked.");
        }

        var activeCount = await _appointments.CountActiveByChildAsync(child.Id, ct);
        if (activeCount >= MaxActiveAppointmentsPerChild)
            return Result<AppointmentResponse>.Fail(ResultStatus.Validation,
                $"A child cannot have more than {MaxActiveAppointmentsPerChild} active appointments.");

        slot.Status = SlotStatus.Booked;
        var appointment = new Appointment
        {
            ChildId = child.Id,
            Child = child,
            SlotId = slot.Id,
            Slot = slot,
            Status = AppointmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _appointments.AddAsync(appointment, ct);

        try
        {
            // שמירה אחת: עדכון המשבצת ויצירת התור באותה טרנזקציה
            await _appointments.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Booking conflict: slot {SlotId} was taken by another request (child {ChildId}).",
                slot.Id, child.Id);
            return Result<AppointmentResponse>.Fail(ResultStatus.Conflict,
                "This slot was just booked by another parent. Please choose a different slot.");
        }

        return Result<AppointmentResponse>.Success(_mapper.Map<AppointmentResponse>(appointment));
    }

    public async Task<Result> CancelAsync(int appointmentId, int userId, Role role, CancellationToken ct)
    {
        var appointment = await _appointments.GetWithDetailsAsync(appointmentId, ct);
        if (appointment is null)
            return Result.Fail(ResultStatus.NotFound, $"Appointment {appointmentId} was not found.");

        if (role != Role.Admin && appointment.Child!.UserId != userId)
            return Result.Fail(ResultStatus.Forbidden, "You can cancel only your own children's appointments.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return Result.Fail(ResultStatus.Validation, "The appointment is already cancelled.");

        // ביטול התור ושחרור המשבצת באותה שמירה
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.Slot!.Status = SlotStatus.Available;

        try
        {
            await _appointments.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Cancellation conflict: slot {SlotId} was modified by another request (appointment {AppointmentId}).",
                appointment.SlotId, appointment.Id);
            return Result.Fail(ResultStatus.Conflict, "The slot was modified by another request. Please try again.");
        }

        return Result.Success();
    }

    public async Task<Result<List<AppointmentResponse>>> GetAsync(int userId, Role role, CancellationToken ct)
    {
        var appointments = await _appointments.GetListAsync(role == Role.Admin ? null : userId, ct);
        return Result<List<AppointmentResponse>>.Success(_mapper.Map<List<AppointmentResponse>>(appointments));
    }
}

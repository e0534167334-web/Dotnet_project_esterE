using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs;

public class BookAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int ChildId { get; set; }

    [Range(1, int.MaxValue)]
    public int SlotId { get; set; }
}

// גוף ה-PATCH: כרגע רק ביטול (Cancelled)
public class UpdateAppointmentRequest
{
    [Required]
    public AppointmentStatus Status { get; set; }
}

public class AppointmentResponse
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int SlotId { get; set; }
    public DateTime SlotStartTimeUtc { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

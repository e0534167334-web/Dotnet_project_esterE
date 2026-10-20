using Core.Enums;

namespace Core.Entities;

public class Appointment
{
    public int Id { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int SlotId { get; set; }
    public Slot? Slot { get; set; }
}

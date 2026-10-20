using Core.Enums;

namespace Core.Entities;

public class Slot
{
    public int Id { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    // Concurrency token - ההגדרה ב-Fluent API בשכבת Data (ממופה ל-xmin)
    public uint Version { get; set; }

    public List<Appointment> Appointments { get; set; } = new();
    public List<VisitType> VisitTypes { get; set; } = new();
}

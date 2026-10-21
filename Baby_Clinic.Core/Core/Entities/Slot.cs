using Core.Enums;

namespace Core.Entities;

public class Slot
{
    public int Id { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    // ===== שלב 7 – תחרות על משאב (1/4): ה-concurrency token =====
    // Concurrency token - ההגדרה ב-Fluent API בשכבת Data (ממופה ל-xmin).
    // כל עדכון של השורה משנה את הערך; שמירה עם ערך ישן נכשלת
    public uint Version { get; set; }

    public List<Appointment> Appointments { get; set; } = new();
    public List<VisitType> VisitTypes { get; set; } = new();
}

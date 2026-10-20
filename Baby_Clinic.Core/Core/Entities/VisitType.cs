namespace Core.Entities;

public class VisitType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Slot> Slots { get; set; } = new();
}

namespace Core.Entities;

public class Child
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}

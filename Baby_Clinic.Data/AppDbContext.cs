using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baby_Clinic.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<VisitType> VisitTypes => Set<VisitType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // כל ההגדרות בקבצים נפרדים תחת Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

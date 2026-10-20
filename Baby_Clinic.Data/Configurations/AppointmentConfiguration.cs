using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baby_Clinic.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        // Restrict: אי אפשר למחוק ילד או משבצת שיש להם תורים
        builder.HasOne(a => a.Child)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Slot)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.SlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ChildId, a.Status });
    }
}

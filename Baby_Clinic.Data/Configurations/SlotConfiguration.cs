using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baby_Clinic.Data.Configurations;

public class SlotConfiguration : IEntityTypeConfiguration<Slot>
{
    public void Configure(EntityTypeBuilder<Slot> builder)
    {
        builder.HasKey(s => s.Id);

        // timestamptz - מקבל רק DateTime מסוג Utc
        builder.Property(s => s.StartTimeUtc).IsRequired();
        builder.HasIndex(s => s.StartTimeUtc);

        builder.Property(s => s.NurseName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        // Concurrency token: uint + IsRowVersion ממופה ע"י Npgsql לעמודת המערכת xmin
        builder.Property(s => s.Version).IsRowVersion();

        // many-to-many: סוגי הביקור שהמשבצת פתוחה אליהם
        builder.HasMany(s => s.VisitTypes)
            .WithMany(v => v.Slots)
            .UsingEntity(j => j.ToTable("SlotVisitTypes"));
    }
}

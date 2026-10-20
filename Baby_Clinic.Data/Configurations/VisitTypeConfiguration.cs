using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baby_Clinic.Data.Configurations;

public class VisitTypeConfiguration : IEntityTypeConfiguration<VisitType>
{
    public void Configure(EntityTypeBuilder<VisitType> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(v => v.Name).IsUnique();
    }
}

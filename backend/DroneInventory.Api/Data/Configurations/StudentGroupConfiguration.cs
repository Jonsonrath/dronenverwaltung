using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.Property(g => g.Name).IsRequired();
        builder.Property(g => g.NormalizedName).IsRequired();

        // Student group names are unique, ignoring case and surrounding whitespace (REQ-003).
        builder.HasIndex(g => g.NormalizedName).IsUnique();

        // Removing the responsible student clears it in the database too (REQ-020, design §1).
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(g => g.ResponsibleStudentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

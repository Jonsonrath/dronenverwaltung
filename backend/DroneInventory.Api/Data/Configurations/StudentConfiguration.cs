using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.Property(s => s.Name).IsRequired();
        builder.Property(s => s.NormalizedName).IsRequired();
        builder.Property(s => s.Class).IsRequired();

        // A group with students cannot be deleted; they are moved or removed first (REQ-012).
        builder.HasOne<StudentGroup>()
            .WithMany()
            .HasForeignKey(s => s.StudentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Student names are unique within a group, ignoring case and whitespace (REQ-016, REQ-018).
        builder.HasIndex(s => new { s.StudentGroupId, s.NormalizedName }).IsUnique();
    }
}

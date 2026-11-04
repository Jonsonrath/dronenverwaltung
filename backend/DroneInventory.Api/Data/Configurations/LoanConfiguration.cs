using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        // Stored as text so the index filter below can compare with 'Active' (design.md §4a).
        // Concurrency token: a return only saves if the status is still the loaded one,
        // so a loan cannot be returned twice at the same time (design.md §4c).
        builder.Property(l => l.Status)
            .HasConversion<string>()
            .IsRequired()
            .IsConcurrencyToken();

        // Dates are stored in UTC; SQLite returns them without a kind, so mark them as UTC on read.
        builder.Property(l => l.LoanDate)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        builder.Property(l => l.ReturnDate)
            .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        builder.HasOne(l => l.Drone).WithMany().HasForeignKey(l => l.DroneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.StudentGroup).WithMany().HasForeignKey(l => l.StudentGroupId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Items).WithOne().HasForeignKey(i => i.LoanId).OnDelete(DeleteBehavior.Cascade);

        // A drone has at most one active loan (REQ-005), guaranteed by the database.
        builder.HasIndex(l => l.DroneId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'")
            .HasDatabaseName("IX_Loans_DroneId_Active");

        builder.Ignore(l => l.Batteries);
        builder.Ignore(l => l.Propellers);
    }
}

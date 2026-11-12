using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class LoanItemConfiguration : IEntityTypeConfiguration<LoanItem>
{
    public void Configure(EntityTypeBuilder<LoanItem> builder)
    {
        builder.Property(i => i.Type).HasConversion<string>().IsRequired();

        // One item per type and loan; quantities are never negative (REQ-006).
        builder.HasIndex(i => new { i.LoanId, i.Type }).IsUnique();
        builder.ToTable("LoanItems", t =>
        {
            t.HasCheckConstraint("CK_LoanItems_Quantity", "\"Quantity\" >= 0");
            // Returned quantity: null until the return, then 0..Quantity (REQ-022).
            t.HasCheckConstraint(
                "CK_LoanItems_ReturnedQuantity",
                "\"ReturnedQuantity\" IS NULL OR (\"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\")");
        });
    }
}

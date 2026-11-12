using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class DroneConfiguration : IEntityTypeConfiguration<Drone>
{
    public void Configure(EntityTypeBuilder<Drone> builder)
    {
        builder.Property(d => d.Name).IsRequired();
        builder.Property(d => d.NormalizedName).IsRequired();
        builder.Property(d => d.Description).IsRequired();

        // Drone names are unique, ignoring case and surrounding whitespace (REQ-001).
        builder.HasIndex(d => d.NormalizedName).IsUnique();

        // A set SSID is unique, ignoring case and surrounding whitespace; many drones may have none (REQ-024).
        builder.HasIndex(d => d.NormalizedSsid).IsUnique().HasFilter("\"NormalizedSsid\" IS NOT NULL");
    }
}

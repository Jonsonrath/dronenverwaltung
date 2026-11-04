using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

// EF Core context for the Drone Lending System. Each entity has its own
// configuration in Data/Configurations and is added with its own migration.
public class DroneInventoryDbContext(DbContextOptions<DroneInventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Drone> Drones => Set<Drone>();

    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DroneInventoryDbContext).Assembly);
}

using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

// EF Core context for the Drone Lending System. Entities and their
// mappings are added by later tasks, each with its own migration.
public class DroneInventoryDbContext(DbContextOptions<DroneInventoryDbContext> options)
    : DbContext(options)
{
}

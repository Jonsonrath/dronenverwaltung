using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class DroneService(DroneInventoryDbContext db)
{
    /// <summary>
    /// Creates a drone (REQ-001). Throws ValidationException for an empty name and
    /// ConflictException if the name already exists, ignoring case and spaces.
    /// </summary>
    public async Task<Drone> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        var drone = new Drone(name, description);

        if (await db.Drones.AnyAsync(d => d.NormalizedName == drone.NormalizedName, cancellationToken))
        {
            throw DuplicateName(drone);
        }

        db.Drones.Add(drone);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request created the same name between the check and the save.
            throw DuplicateName(drone);
        }

        return drone;
    }

    /// <summary>All drones, ordered by name (REQ-002). The derived status follows in task 4.7.</summary>
    public async Task<IReadOnlyList<Drone>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Drones
            .AsNoTracking()
            .OrderBy(d => d.NormalizedName)
            .ToListAsync(cancellationToken);

    private static ConflictException DuplicateName(Drone drone) =>
        new($"A drone with the name \"{drone.Name}\" already exists (names are compared ignoring case).");
}

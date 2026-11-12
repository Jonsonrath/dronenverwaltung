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

    /// <summary>
    /// Edits name and description of a drone (REQ-013) and returns it with its derived status.
    /// Throws NotFoundException for an unknown id, ValidationException for an empty name and
    /// ConflictException if another drone has the name.
    /// </summary>
    public async Task<(Drone Drone, DroneStatus Status)> UpdateAsync(
        int id, string name, string? description, CancellationToken cancellationToken = default)
    {
        var drone = await db.Drones.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Die Drohne {id} gibt es nicht.");
        drone.Edit(name, description);

        // Only other drones count: changing the spelling of the own name is allowed.
        if (await db.Drones.AnyAsync(d => d.Id != id && d.NormalizedName == drone.NormalizedName, cancellationToken))
        {
            throw DuplicateName(drone);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request took the name between the check and the save.
            throw DuplicateName(drone);
        }

        var isLoaned = await db.Loans.AnyAsync(l => l.DroneId == id && l.Status == LoanStatus.Active, cancellationToken);
        return (drone, DroneStatusRules.Derive(isLoaned, drone.DamageReason));
    }

    /// <summary>
    /// All drones, ordered by name, each with its status derived from the loans and the damage
    /// (REQ-002): Loaned if it has an active loan, otherwise Damaged if damaged, otherwise
    /// Available. No status is stored.
    /// </summary>
    public async Task<IReadOnlyList<(Drone Drone, DroneStatus Status)>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Drones
            .AsNoTracking()
            .OrderBy(d => d.NormalizedName)
            .Select(d => new
            {
                Drone = d,
                IsLoaned = db.Loans.Any(l => l.DroneId == d.Id && l.Status == LoanStatus.Active),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Drone, DroneStatusRules.Derive(r.IsLoaned, r.Drone.DamageReason))).ToList();
    }

    private static ConflictException DuplicateName(Drone drone) =>
        new($"Eine Drohne mit dem Namen \"{drone.Name}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");
}

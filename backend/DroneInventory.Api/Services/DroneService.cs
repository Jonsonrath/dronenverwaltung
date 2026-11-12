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
        var drone = await FindAsync(id, cancellationToken);
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

        return (drone, await StatusOfAsync(drone, cancellationToken));
    }

    /// <summary>
    /// Marks a drone as damaged (REQ-014); a new reason replaces the old one. A loaned drone
    /// stays loaned, its loan is not touched. Throws NotFoundException for an unknown id and
    /// ValidationException for an empty reason.
    /// </summary>
    public async Task<(Drone Drone, DroneStatus Status)> ReportDamageAsync(
        int id, string? reason, CancellationToken cancellationToken = default)
    {
        var drone = await FindAsync(id, cancellationToken);
        drone.ReportDamage(reason);
        await db.SaveChangesAsync(cancellationToken);
        return (drone, await StatusOfAsync(drone, cancellationToken));
    }

    /// <summary>
    /// Marks a damaged drone as repaired (REQ-015) and removes the reason. Throws
    /// NotFoundException for an unknown id and ConflictException if the drone is not damaged.
    /// </summary>
    public async Task<(Drone Drone, DroneStatus Status)> RepairAsync(int id, CancellationToken cancellationToken = default)
    {
        var drone = await FindAsync(id, cancellationToken);
        drone.Repair();
        await db.SaveChangesAsync(cancellationToken);
        return (drone, await StatusOfAsync(drone, cancellationToken));
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

    private async Task<Drone> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Drones.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Die Drohne {id} gibt es nicht.");

    private async Task<DroneStatus> StatusOfAsync(Drone drone, CancellationToken cancellationToken)
    {
        var isLoaned = await db.Loans.AnyAsync(l => l.DroneId == drone.Id && l.Status == LoanStatus.Active, cancellationToken);
        return DroneStatusRules.Derive(isLoaned, drone.DamageReason);
    }

    private static ConflictException DuplicateName(Drone drone) =>
        new($"Eine Drohne mit dem Namen \"{drone.Name}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");
}

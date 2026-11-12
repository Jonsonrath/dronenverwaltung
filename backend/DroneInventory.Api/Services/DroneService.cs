using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class DroneService(DroneInventoryDbContext db)
{
    /// <summary>
    /// Creates a drone (REQ-001). Throws ValidationException for an empty name and
    /// ConflictException if the name or the set SSID (REQ-024) already exists, ignoring case and spaces.
    /// </summary>
    public async Task<Drone> CreateAsync(
        string name, string? description, string? ssid = null, CancellationToken cancellationToken = default)
    {
        var drone = new Drone(name, description, ssid);

        if (await db.Drones.AnyAsync(d => d.NormalizedName == drone.NormalizedName, cancellationToken))
        {
            throw DuplicateName(drone);
        }

        await EnsureSsidIsFree(drone, cancellationToken);

        db.Drones.Add(drone);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request created the same name or SSID between the check and the save.
            throw Duplicate(drone, exception);
        }

        return drone;
    }

    /// <summary>
    /// Edits name, description and SSID of a drone (REQ-013, REQ-024) and returns it with its derived status.
    /// Throws NotFoundException for an unknown id, ValidationException for an empty name and
    /// ConflictException if another drone has the name or the set SSID.
    /// </summary>
    public async Task<(Drone Drone, DroneStatus Status)> UpdateAsync(
        int id, string name, string? description, string? ssid = null, CancellationToken cancellationToken = default)
    {
        var drone = await db.Drones.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Die Drohne {id} gibt es nicht.");
        drone.Edit(name, description, ssid);

        // Only other drones count: changing the spelling of the own name is allowed.
        if (await db.Drones.AnyAsync(d => d.Id != id && d.NormalizedName == drone.NormalizedName, cancellationToken))
        {
            throw DuplicateName(drone);
        }

        await EnsureSsidIsFree(drone, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request took the name or SSID between the check and the save.
            throw Duplicate(drone, exception);
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

    // Only other drones count, so a drone keeps its own SSID when it is edited (REQ-024).
    private async Task EnsureSsidIsFree(Drone drone, CancellationToken cancellationToken)
    {
        if (drone.NormalizedSsid is not null
            && await db.Drones.AnyAsync(d => d.Id != drone.Id && d.NormalizedSsid == drone.NormalizedSsid, cancellationToken))
        {
            throw DuplicateSsid(drone);
        }
    }

    // SQLite names the violated column, e.g. "UNIQUE constraint failed: Drones.NormalizedSsid".
    private static ConflictException Duplicate(Drone drone, DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("NormalizedSsid", StringComparison.Ordinal) == true
            ? DuplicateSsid(drone)
            : DuplicateName(drone);

    private static ConflictException DuplicateSsid(Drone drone) =>
        new($"Eine Drohne mit der SSID \"{drone.Ssid}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");

    private static ConflictException DuplicateName(Drone drone) =>
        new($"Eine Drohne mit dem Namen \"{drone.Name}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");
}

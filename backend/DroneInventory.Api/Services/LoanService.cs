using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Services;

public class LoanService(DroneInventoryDbContext db, TimeProvider time)
{
    /// <summary>
    /// Loans a drone to a student group with batteries and propellers (REQ-004, REQ-006).
    /// Throws NotFoundException for an unknown drone or group and ValidationException
    /// for a negative quantity. The LoanDate is set by the system.
    /// </summary>
    public async Task<Loan> CreateAsync(
        int droneId, int studentGroupId, int batteries, int propellers, CancellationToken cancellationToken = default)
    {
        var drone = await db.Drones.FindAsync([droneId], cancellationToken)
            ?? throw new NotFoundException($"Drone {droneId} does not exist.");
        var group = await db.StudentGroups.FindAsync([studentGroupId], cancellationToken)
            ?? throw new NotFoundException($"Student group {studentGroupId} does not exist.");

        var loan = new Loan(drone, group, batteries, propellers, time.GetUtcNow().UtcDateTime);

        db.Loans.Add(loan);
        await db.SaveChangesAsync(cancellationToken);
        return loan;
    }
}

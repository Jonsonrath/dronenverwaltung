using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class LoanService(DroneInventoryDbContext db, TimeProvider time)
{
    /// <summary>
    /// Loans a drone to a student group with batteries and propellers (REQ-004, REQ-006).
    /// Throws NotFoundException for an unknown drone or group, ValidationException
    /// for a negative quantity and ConflictException if the drone already has an
    /// active loan (REQ-005). The LoanDate is set by the system.
    /// </summary>
    public async Task<Loan> CreateAsync(
        int droneId, int studentGroupId, int batteries, int propellers, CancellationToken cancellationToken = default)
    {
        var drone = await db.Drones.FindAsync([droneId], cancellationToken)
            ?? throw new NotFoundException($"Drone {droneId} does not exist.");
        var group = await db.StudentGroups.FindAsync([studentGroupId], cancellationToken)
            ?? throw new NotFoundException($"Student group {studentGroupId} does not exist.");

        var loan = new Loan(drone, group, batteries, propellers, time.GetUtcNow().UtcDateTime);

        if (await db.Loans.AnyAsync(l => l.DroneId == droneId && l.Status == LoanStatus.Active, cancellationToken))
        {
            throw AlreadyLoaned(drone);
        }

        db.Loans.Add(loan);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request loaned the drone between the check and the save; the
            // filtered unique index IX_Loans_DroneId_Active stopped it (design.md §4a).
            throw AlreadyLoaned(drone);
        }

        return loan;
    }

    private static ConflictException AlreadyLoaned(Drone drone) =>
        new($"Drone \"{drone.Name}\" already has an active loan.");
}

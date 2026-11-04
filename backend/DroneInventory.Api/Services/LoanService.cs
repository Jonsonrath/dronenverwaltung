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

    /// <summary>
    /// Returns a loan (REQ-008, REQ-009): Active → Returned with the current date as
    /// ReturnDate; the drone becomes available again because it has no active loan.
    /// Throws NotFoundException for an unknown loan and ConflictException if the loan
    /// has already been returned. The loan itself is kept (REQ-010).
    /// </summary>
    public async Task<Loan> ReturnAsync(int loanId, CancellationToken cancellationToken = default)
    {
        var loan = await db.Loans
            .Include(l => l.Drone)
            .Include(l => l.StudentGroup)
            .Include(l => l.Items)
            .SingleOrDefaultAsync(l => l.Id == loanId, cancellationToken)
            ?? throw new NotFoundException($"Loan {loanId} does not exist.");

        loan.Return(time.GetUtcNow().UtcDateTime);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request returned the loan between our read and our save; its
            // ReturnDate stays (Status is a concurrency token, design.md §4c).
            throw new ConflictException($"Loan {loanId} has already been returned.");
        }

        return loan;
    }

    /// <summary>
    /// Loans with the given status, with drone, group and accessories.
    /// Active (REQ-007): oldest loan first. Returned = loan history (REQ-010):
    /// most recent return first. Returned loans are never deleted, so the history is complete.
    /// </summary>
    public async Task<IReadOnlyList<Loan>> ListAsync(LoanStatus status, CancellationToken cancellationToken = default)
    {
        var query = db.Loans
            .AsNoTracking()
            .Include(l => l.Drone)
            .Include(l => l.StudentGroup)
            .Include(l => l.Items)
            .Where(l => l.Status == status);

        var ordered = status == LoanStatus.Returned
            ? query.OrderByDescending(l => l.ReturnDate).ThenBy(l => l.Drone.NormalizedName)
            : query.OrderBy(l => l.LoanDate).ThenBy(l => l.Drone.NormalizedName);

        return await ordered.ToListAsync(cancellationToken);
    }

    private static ConflictException AlreadyLoaned(Drone drone) =>
        new($"Drone \"{drone.Name}\" already has an active loan.");
}

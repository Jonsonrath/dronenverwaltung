using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace DroneInventory.Api.Tests.Services;

// Task 4.3: LoanService and the race between the active-loan check and the save (design.md §4a).
public class LoanServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();
    private readonly int _droneId;
    private readonly int _group1Id;
    private readonly int _group3Id;

    public LoanServiceTests()
    {
        using var context = _database.CreateContext();
        var drone = new Drone("TELLO-07", null);
        var group1 = new StudentGroup("Group 1");
        var group3 = new StudentGroup("Group 3");
        context.AddRange(drone, group1, group3);
        context.SaveChanges();
        (_droneId, _group1Id, _group3Id) = (drone.Id, group1.Id, group3.Id);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task REQ_005_ActiveLoanSavedConcurrently_ThrowsConflictInsteadOfDatabaseError()
    {
        // Simulates a second request that loans the same drone after our
        // active-loan check but before our SaveChanges.
        await using var context = _database.CreateContext(new LoanSameDroneBeforeSave(this));
        var service = new LoanService(context, new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(_droneId, _group3Id, 4, 8));

        await using var check = _database.CreateContext();
        var loan = Assert.Single(check.Loans);
        Assert.Equal(_group1Id, loan.StudentGroupId);
    }

    [Fact]
    public async Task REQ_008_LoanReturnedConcurrently_ThrowsConflictAndKeepsFirstReturnDate()
    {
        int loanId;
        await using (var setup = _database.CreateContext())
        {
            var loan = await new LoanService(setup, new FakeTimeProvider(Now)).CreateAsync(_droneId, _group3Id, 4, 8);
            loanId = loan.Id;
        }

        // Simulates a second request that returns the same loan after our read
        // but before our SaveChanges.
        var firstReturnDate = Now.UtcDateTime.AddDays(7);
        await using var context = _database.CreateContext(new ReturnLoanBeforeSave(_database, loanId, firstReturnDate));
        var service = new LoanService(context, new FakeTimeProvider(Now.AddDays(8)));

        await Assert.ThrowsAsync<ConflictException>(() => service.ReturnAsync(loanId));

        await using var check = _database.CreateContext();
        var stored = await check.Loans.SingleAsync(l => l.Id == loanId);
        Assert.Equal(LoanStatus.Returned, stored.Status);
        Assert.Equal(firstReturnDate, stored.ReturnDate);
    }

    private sealed class ReturnLoanBeforeSave(TestDatabase database, int loanId, DateTime returnDate) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            var loan = await other.Loans.SingleAsync(l => l.Id == loanId, cancellationToken);
            loan.Return(returnDate);
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class LoanSameDroneBeforeSave(LoanServiceTests test) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = test._database.CreateContext();
            var drone = await other.Drones.SingleAsync(d => d.Id == test._droneId, cancellationToken);
            var group = await other.StudentGroups.SingleAsync(g => g.Id == test._group1Id, cancellationToken);
            other.Loans.Add(new Loan(drone, group, 1, 1, Now.UtcDateTime));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

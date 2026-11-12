using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace DroneInventory.Api.Tests.Services;

// Change damage-report-on-return, task 1.1: "Report damage on return (REQ-023)" in
// LoanService.ReturnAsync – loan and drone are saved in the same SaveChanges (design §1, §3).
public class DamageReportOnReturnServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();
    private readonly int _droneId;
    private readonly int _group1Id;
    private readonly int _loanId;

    public DamageReportOnReturnServiceTests()
    {
        using var context = _database.CreateContext();
        var drone = new Drone("TELLO-07", null);
        var group1 = new StudentGroup("Group 1");
        var group3 = new StudentGroup("Group 3");
        context.AddRange(drone, group1, group3);
        context.SaveChanges();
        var loan = new Loan(drone, group3, 4, 8, Now.UtcDateTime);
        context.Loans.Add(loan);
        context.SaveChanges();
        (_droneId, _group1Id, _loanId) = (drone.Id, group1.Id, loan.Id);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task REQ_023_ReturnWithDamageReport_StoresReportAndDamagesDrone()
    {
        await ReturnAsync(" Propeller gebrochen ");

        await using var check = _database.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.Id == _loanId);
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal("Propeller gebrochen", loan.DamageReport);
        var drone = await check.Drones.SingleAsync(d => d.Id == _droneId);
        Assert.Equal("Propeller gebrochen", drone.DamageReason);
    }

    [Fact]
    public async Task REQ_023_ReturnWithoutDamageReport_NoReportAndDroneNotDamaged()
    {
        await ReturnAsync(null);

        await AssertReturnedWithoutReportAndDroneNotDamaged();
    }

    [Fact]
    public async Task REQ_023_WhitespaceOnlyReport_CountsAsNoReport()
    {
        await ReturnAsync("   ");

        await AssertReturnedWithoutReportAndDroneNotDamaged();
    }

    [Fact]
    public async Task REQ_023_DamagedDroneCannotBeLoanedAfterTheReturn()
    {
        await ReturnAsync("Propeller gebrochen");

        await using var context = _database.CreateContext();
        var service = new LoanService(context, new FakeTimeProvider(Now.AddDays(1)));
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(_droneId, _group1Id, 0, 0));
        Assert.Equal("Die Drohne \"TELLO-07\" ist defekt: Propeller gebrochen", exception.Message);
    }

    [Fact]
    public async Task REQ_023_AlreadyDamagedDrone_ReportOnReturnReplacesReason()
    {
        await using (var setup = _database.CreateContext())
        {
            var drone = await setup.Drones.SingleAsync(d => d.Id == _droneId);
            drone.ReportDamage("Akku bläht sich auf");
            await setup.SaveChangesAsync();
        }

        await ReturnAsync("Propeller gebrochen");

        await using var check = _database.CreateContext();
        Assert.Equal("Propeller gebrochen", (await check.Drones.SingleAsync(d => d.Id == _droneId)).DamageReason);
    }

    [Fact]
    public async Task REQ_023_InvalidReturnedQuantity_LoanStaysActiveAndDroneNotDamaged()
    {
        await using (var context = _database.CreateContext())
        {
            var service = new LoanService(context, new FakeTimeProvider(Now.AddDays(1)));
            await Assert.ThrowsAsync<ValidationException>(
                () => service.ReturnAsync(_loanId, 5, null, "Propeller gebrochen"));
        }

        await using var check = _database.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.Id == _loanId);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Null(loan.DamageReport);
        Assert.Null((await check.Drones.SingleAsync(d => d.Id == _droneId)).DamageReason);
    }

    [Fact]
    public async Task REQ_023_LoanReturnedConcurrently_DroneIsNotDamaged()
    {
        // A second request returns the loan (without report) after our read but before our
        // save; our SaveChanges fails as a whole, so the drone must not be damaged either.
        await using (var context = _database.CreateContext(new ReturnLoanBeforeSave(_database, _loanId)))
        {
            var service = new LoanService(context, new FakeTimeProvider(Now.AddDays(1)));
            await Assert.ThrowsAsync<ConflictException>(
                () => service.ReturnAsync(_loanId, damageReport: "Propeller gebrochen"));
        }

        await AssertReturnedWithoutReportAndDroneNotDamaged();
    }

    private async Task ReturnAsync(string? damageReport)
    {
        await using var context = _database.CreateContext();
        var service = new LoanService(context, new FakeTimeProvider(Now.AddDays(1)));
        await service.ReturnAsync(_loanId, damageReport: damageReport);
    }

    private async Task AssertReturnedWithoutReportAndDroneNotDamaged()
    {
        await using var check = _database.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.Id == _loanId);
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Null(loan.DamageReport);
        Assert.Null((await check.Drones.SingleAsync(d => d.Id == _droneId)).DamageReason);
        Assert.False(await check.Loans.AnyAsync(l => l.DroneId == _droneId && l.Status == LoanStatus.Active));
    }

    private sealed class ReturnLoanBeforeSave(TestDatabase database, int loanId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            var loan = await other.Loans.SingleAsync(l => l.Id == loanId, cancellationToken);
            loan.Return(Now.UtcDateTime.AddHours(1));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

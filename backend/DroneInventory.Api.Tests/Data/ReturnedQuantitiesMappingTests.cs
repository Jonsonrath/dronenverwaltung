using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Change return-differences, task 1.1: migration AddReturnedQuantities (REQ-022).
public class ReturnedQuantitiesMappingTests : IDisposable
{
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly int _loanId;

    public ReturnedQuantitiesMappingTests()
    {
        using var context = _database.CreateContext();
        var loan = new Loan(new Drone("TELLO-07", null), new StudentGroup("Group 3"), 4, 8, LoanDate);
        context.Loans.Add(loan);
        context.SaveChanges();
        _loanId = loan.Id;
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void REQ_022_ActiveLoanStoresNullReturnedQuantities()
    {
        using var context = _database.CreateContext();
        var stored = context.Loans.Include(l => l.Items).Single(l => l.Id == _loanId);

        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_AddReturnedQuantities"));
        Assert.Null(stored.ReturnedBatteries);
        Assert.Null(stored.ReturnedPropellers);
    }

    [Fact]
    public void REQ_022_StoresAndLoadsReturnedQuantities()
    {
        using (var context = _database.CreateContext())
        {
            context.Loans.Include(l => l.Items).Single(l => l.Id == _loanId).Return(LoanDate.AddDays(7), 3, 8);
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        var stored = readContext.Loans.Include(l => l.Items).Single(l => l.Id == _loanId);
        Assert.Equal(3, stored.ReturnedBatteries);
        Assert.Equal(8, stored.ReturnedPropellers);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void REQ_022_DatabaseRejectsReturnedQuantityOutsideZeroToQuantity(int returned)
    {
        using var context = _database.CreateContext();

        Assert.Throws<SqliteException>(() => context.Database.ExecuteSqlRaw(
            "UPDATE LoanItems SET ReturnedQuantity = {0} WHERE LoanId = {1} AND Type = 'Battery'", returned, _loanId));
    }
}

using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Task 4.1: migration AddLoans, enum mapping, cascade and the one-active-loan index (REQ-005).
public class LoanMappingTests : IDisposable
{
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly int _droneId;
    private readonly int _otherDroneId;
    private readonly int _groupId;

    public LoanMappingTests()
    {
        using var context = _database.CreateContext();
        var drone = new Drone("TELLO-07", null);
        var otherDrone = new Drone("TELLO-02", null);
        var group = new StudentGroup("Group 3");
        context.AddRange(drone, otherDrone, group);
        context.SaveChanges();
        (_droneId, _otherDroneId, _groupId) = (drone.Id, otherDrone.Id, group.Id);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void AddLoansCreatesLoansAndLoanItemsTables()
    {
        using var context = _database.CreateContext();

        var tables = context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToList();

        Assert.Contains("Loans", tables);
        Assert.Contains("LoanItems", tables);
        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_AddLoans"));
    }

    [Fact]
    public void REQ_006_StoresAndLoadsLoanWithItems()
    {
        int loanId;
        using (var context = _database.CreateContext())
        {
            var loan = NewLoan(context, _droneId, 4, 8);
            context.SaveChanges();
            loanId = loan.Id;
        }

        using var readContext = _database.CreateContext();
        var stored = readContext.Loans
            .Include(l => l.Items)
            .Include(l => l.Drone)
            .Include(l => l.StudentGroup)
            .Single(l => l.Id == loanId);
        Assert.Equal(LoanStatus.Active, stored.Status);
        Assert.Equal(LoanDate, stored.LoanDate);
        Assert.Null(stored.ReturnDate);
        Assert.Equal("TELLO-07", stored.Drone.Name);
        Assert.Equal("Group 3", stored.StudentGroup.Name);
        Assert.Equal(4, stored.Batteries);
        Assert.Equal(8, stored.Propellers);
    }

    [Fact]
    public void REQ_009_DatesAreLoadedAsUtc()
    {
        int loanId;
        using (var context = _database.CreateContext())
        {
            var loan = NewLoan(context, _droneId, 1, 1);
            context.SaveChanges();
            loan.Return(LoanDate.AddDays(7));
            context.SaveChanges();
            loanId = loan.Id;
        }

        using var readContext = _database.CreateContext();
        var stored = readContext.Loans.Single(l => l.Id == loanId);
        Assert.Equal(DateTimeKind.Utc, stored.LoanDate.Kind);
        Assert.Equal(DateTimeKind.Utc, stored.ReturnDate!.Value.Kind);
        Assert.Equal(LoanDate.AddDays(7), stored.ReturnDate);
    }

    [Fact]
    public void EnumsAreStoredAsText()
    {
        using var context = _database.CreateContext();
        NewLoan(context, _droneId, 4, 8);
        context.SaveChanges();

        var status = context.Database.SqlQueryRaw<string>("SELECT Status AS Value FROM Loans").Single();
        var types = context.Database.SqlQueryRaw<string>("SELECT Type AS Value FROM LoanItems ORDER BY Type").ToList();

        Assert.Equal("Active", status);
        Assert.Equal(["Battery", "Propeller"], types);
    }

    [Fact]
    public void REQ_005_SecondActiveLoanForSameDroneIsRejectedByDatabase()
    {
        using var context = _database.CreateContext();
        NewLoan(context, _droneId, 1, 1);
        context.SaveChanges();

        NewLoan(context, _droneId, 2, 2);
        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsUniqueConstraintViolation());
    }

    [Fact]
    public void REQ_005_ActiveAndReturnedLoanForSameDroneAreAllowed()
    {
        using var context = _database.CreateContext();
        var first = NewLoan(context, _droneId, 1, 1);
        context.SaveChanges();
        first.Return(LoanDate.AddDays(7));
        context.SaveChanges();

        NewLoan(context, _droneId, 2, 2);
        context.SaveChanges();

        Assert.Equal(2, context.Loans.Count(l => l.DroneId == _droneId));
    }

    [Fact]
    public void REQ_005_ActiveLoansForDifferentDronesAreAllowed()
    {
        using var context = _database.CreateContext();
        NewLoan(context, _droneId, 1, 1);
        NewLoan(context, _otherDroneId, 1, 1);

        context.SaveChanges();

        Assert.Equal(2, context.Loans.Count(l => l.Status == LoanStatus.Active));
    }

    [Fact]
    public void REQ_006_DatabaseRejectsNegativeQuantity()
    {
        using var context = _database.CreateContext();
        var loan = NewLoan(context, _droneId, 1, 1);
        context.SaveChanges();

        Assert.Throws<SqliteException>(() => context.Database.ExecuteSqlRaw(
            "UPDATE LoanItems SET Quantity = -1 WHERE LoanId = {0}", loan.Id));
    }

    [Fact]
    public void LoanItemsAreDeletedWithTheirLoan()
    {
        using var context = _database.CreateContext();
        var itemsForeignKey = context.Model
            .FindEntityType(typeof(LoanItem))!
            .GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Loan));

        Assert.Equal(DeleteBehavior.Cascade, itemsForeignKey.DeleteBehavior);
    }

    private Loan NewLoan(DroneInventoryDbContext context, int droneId, int batteries, int propellers)
    {
        var loan = new Loan(context.Drones.Find(droneId)!, context.StudentGroups.Find(_groupId)!, batteries, propellers, LoanDate);
        context.Loans.Add(loan);
        return loan;
    }
}

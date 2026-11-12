using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Domain;

// Change return-differences, task 1.1: "Record returned accessories (REQ-022)" in the Loan aggregate.
public class ReturnedQuantitiesTests
{
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReturnDate = new(2026, 8, 26, 14, 30, 0, DateTimeKind.Utc);

    private readonly Drone _drone = new("TELLO-07", null);
    private readonly StudentGroup _group = new("Group 3");

    [Fact]
    public void REQ_022_ActiveLoanHasNoReturnedQuantities()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        Assert.Null(loan.ReturnedBatteries);
        Assert.Null(loan.ReturnedPropellers);
    }

    [Fact]
    public void REQ_022_CompleteReturn_WithoutQuantitiesEverythingCountsAsReturned()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        loan.Return(ReturnDate);

        Assert.Equal(4, loan.ReturnedBatteries);
        Assert.Equal(8, loan.ReturnedPropellers);
    }

    [Fact]
    public void REQ_022_MissingBattery_StoresReturnedQuantities()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        loan.Return(ReturnDate, returnedBatteries: 3, returnedPropellers: 8);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(3, loan.ReturnedBatteries);
        Assert.Equal(8, loan.ReturnedPropellers);
        // The loaned quantities stay unchanged.
        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
    }

    [Fact]
    public void REQ_022_OnlyOneQuantityGiven_OtherCountsAsLoaned()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        loan.Return(ReturnDate, returnedBatteries: null, returnedPropellers: 6);

        Assert.Equal(4, loan.ReturnedBatteries);
        Assert.Equal(6, loan.ReturnedPropellers);
    }

    [Fact]
    public void REQ_022_ZeroReturnedIsAllowed()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        loan.Return(ReturnDate, 0, 0);

        Assert.Equal(0, loan.ReturnedBatteries);
        Assert.Equal(0, loan.ReturnedPropellers);
    }

    [Theory]
    [InlineData(5, null, "Die Anzahl der zurückgegebenen Akkus muss zwischen 0 und 4 liegen.")]
    [InlineData(null, -1, "Die Anzahl der zurückgegebenen Propeller muss zwischen 0 und 8 liegen.")]
    [InlineData(-1, null, "Die Anzahl der zurückgegebenen Akkus muss zwischen 0 und 4 liegen.")]
    [InlineData(null, 9, "Die Anzahl der zurückgegebenen Propeller muss zwischen 0 und 8 liegen.")]
    public void REQ_022_MoreThanLoanedOrNegative_ThrowsAndLoanStaysActive(int? batteries, int? propellers, string message)
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        var error = Assert.Throws<ValidationException>(() => loan.Return(ReturnDate, batteries, propellers));

        Assert.Equal(message, error.Message);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Null(loan.ReturnDate);
        Assert.Null(loan.ReturnedBatteries);
        Assert.Null(loan.ReturnedPropellers);
    }

    [Fact]
    public void REQ_022_InvalidPropellersDoNotStoreValidBatteries()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        Assert.Throws<ValidationException>(() => loan.Return(ReturnDate, 3, -1));

        Assert.Null(loan.ReturnedBatteries);
    }

    [Fact]
    public void REQ_008_ReturningTwiceKeepsFirstReturnedQuantities()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);
        loan.Return(ReturnDate, 3, 8);

        Assert.Throws<ConflictException>(() => loan.Return(ReturnDate.AddDays(1), 4, 8));

        Assert.Equal(3, loan.ReturnedBatteries);
    }
}

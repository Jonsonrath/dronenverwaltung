using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Domain;

// Task 4.1, spec loan: the Loan aggregate (REQ-004, REQ-006, REQ-008, REQ-009).
public class LoanTests
{
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReturnDate = new(2026, 8, 26, 14, 30, 0, DateTimeKind.Utc);

    private readonly Drone _drone = new("TELLO-07", null);
    private readonly StudentGroup _group = new("Group 3");

    [Fact]
    public void REQ_004_NewLoanIsActiveWithLoanDateAndNoReturnDate()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(LoanDate, loan.LoanDate);
        Assert.Null(loan.ReturnDate);
        Assert.Same(_drone, loan.Drone);
        Assert.Same(_group, loan.StudentGroup);
    }

    [Fact]
    public void REQ_006_LoanContainsBatteriesAndPropellersAsQuantities()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
        Assert.Equal([LoanItemType.Battery, LoanItemType.Propeller], loan.Items.Select(i => i.Type));
    }

    [Fact]
    public void REQ_006_LoanWithoutAccessoriesHasZeroQuantities()
    {
        var loan = new Loan(_drone, _group, 0, 0, LoanDate);

        Assert.Equal(0, loan.Batteries);
        Assert.Equal(0, loan.Propellers);
        Assert.Equal(2, loan.Items.Count);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void REQ_006_NegativeQuantityThrows(int batteries, int propellers)
    {
        Assert.Throws<ValidationException>(() => new Loan(_drone, _group, batteries, propellers, LoanDate));
    }

    [Fact]
    public void REQ_008_REQ_009_ReturnMarksLoanReturnedAndStoresReturnDate()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);

        loan.Return(ReturnDate);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(ReturnDate, loan.ReturnDate);
        // Everything else is unchanged (REQ-009).
        Assert.Equal(LoanDate, loan.LoanDate);
        Assert.Same(_drone, loan.Drone);
        Assert.Same(_group, loan.StudentGroup);
        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
    }

    [Fact]
    public void REQ_008_ReturningTwiceThrowsAndKeepsFirstReturnDate()
    {
        var loan = new Loan(_drone, _group, 4, 8, LoanDate);
        loan.Return(ReturnDate);

        Assert.Throws<ConflictException>(() => loan.Return(ReturnDate.AddDays(1)));

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(ReturnDate, loan.ReturnDate);
    }
}

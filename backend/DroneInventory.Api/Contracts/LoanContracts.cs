using System.ComponentModel.DataAnnotations;
using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>
/// Body of POST /api/loans (REQ-004, REQ-006). Drone and student group are required;
/// a missing number of batteries or propellers counts as 0.
/// </summary>
public record CreateLoanRequest(
    [Required] int? DroneId,
    [Required] int? StudentGroupId,
    int? Batteries,
    int? Propellers);

/// <summary>A loan as returned by the API, flat with drone and group names (design.md §6).</summary>
public record LoanResponse(
    int Id,
    int DroneId,
    string DroneName,
    int StudentGroupId,
    string StudentGroupName,
    int Batteries,
    int Propellers,
    DateTime LoanDate,
    DateTime? ReturnDate,
    LoanStatus Status)
{
    public static LoanResponse From(Loan loan) => new(
        loan.Id,
        loan.DroneId,
        loan.Drone.Name,
        loan.StudentGroupId,
        loan.StudentGroup.Name,
        loan.Batteries,
        loan.Propellers,
        loan.LoanDate,
        loan.ReturnDate,
        loan.Status);
}

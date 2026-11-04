using System.ComponentModel.DataAnnotations;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController(LoanService loans) : ControllerBase
{
    /// <summary>
    /// Loans with the given status: <c>?status=Active</c> lists the active loans (REQ-007),
    /// <c>?status=Returned</c> the loan history (REQ-010). A missing or unknown status gives 422.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IReadOnlyList<LoanResponse>> List([FromQuery, Required] LoanStatus? status, CancellationToken cancellationToken)
    {
        var all = await loans.ListAsync(status!.Value, cancellationToken);
        return all.Select(LoanResponse.From).ToList();
    }

    /// <summary>Loan a drone to a student group with accessories (REQ-004, REQ-006).</summary>
    [HttpPost]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var loan = await loans.CreateAsync(
            request.DroneId!.Value,
            request.StudentGroupId!.Value,
            request.Batteries ?? 0,
            request.Propellers ?? 0,
            cancellationToken);

        // No single-loan endpoint exists, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, LoanResponse.From(loan));
    }

    /// <summary>
    /// Return a loan (REQ-008, REQ-009). A POST action and not DELETE, because the
    /// loan is kept as history (REQ-010).
    /// </summary>
    [HttpPost("{id:int}/return")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<LoanResponse> Return(int id, CancellationToken cancellationToken) =>
        LoanResponse.From(await loans.ReturnAsync(id, cancellationToken));
}

using DroneInventory.Api.Contracts;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController(LoanService loans) : ControllerBase
{
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
}

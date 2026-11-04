using DroneInventory.Api.Contracts;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/student-groups")]
public class StudentGroupsController(StudentGroupService groups) : ControllerBase
{
    /// <summary>List all student groups, ordered by name (REQ-003).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentGroupResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<StudentGroupResponse>> List(CancellationToken cancellationToken)
    {
        var all = await groups.ListAsync(cancellationToken);
        return all.Select(StudentGroupResponse.From).ToList();
    }

    /// <summary>Create a student group (REQ-003).</summary>
    [HttpPost]
    [ProducesResponseType<StudentGroupResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(CreateStudentGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await groups.CreateAsync(request.Name ?? string.Empty, cancellationToken);

        // No single-group endpoint exists, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, StudentGroupResponse.From(group));
    }
}

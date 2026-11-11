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

    /// <summary>Rename a student group (REQ-011).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<StudentGroupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<StudentGroupResponse> Rename(
        int id, RenameStudentGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await groups.RenameAsync(id, request.Name ?? string.Empty, cancellationToken);
        return StudentGroupResponse.From(group);
    }

    /// <summary>Delete a student group that has no loan (REQ-012).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await groups.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

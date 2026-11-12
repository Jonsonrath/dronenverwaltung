using DroneInventory.Api.Contracts;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/student-groups")]
public class StudentGroupsController(StudentGroupService groups, StudentService students) : ControllerBase
{
    /// <summary>List all student groups with their students, both ordered by name (REQ-003, REQ-017).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentGroupListResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<StudentGroupListResponse>> List(CancellationToken cancellationToken)
    {
        var all = await groups.ListAsync(cancellationToken);
        var members = await students.ListByGroupAsync(cancellationToken);
        return all.Select(g => StudentGroupListResponse.From(g, members[g.Id])).ToList();
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

    /// <summary>Mark a student of the group as its responsible student (REQ-020).</summary>
    [HttpPut("{id:int}/responsible")]
    [ProducesResponseType<StudentGroupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<StudentGroupResponse> SetResponsible(
        int id, SetResponsibleStudentRequest request, CancellationToken cancellationToken)
    {
        var group = await groups.SetResponsibleAsync(id, request.StudentId!.Value, cancellationToken);
        return StudentGroupResponse.From(group);
    }

    /// <summary>Clear the responsible student of the group (REQ-020).</summary>
    [HttpDelete("{id:int}/responsible")]
    [ProducesResponseType<StudentGroupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<StudentGroupResponse> ClearResponsible(int id, CancellationToken cancellationToken)
    {
        var group = await groups.ClearResponsibleAsync(id, cancellationToken);
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

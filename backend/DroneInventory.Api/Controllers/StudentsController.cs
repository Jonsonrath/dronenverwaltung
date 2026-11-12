using DroneInventory.Api.Contracts;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
public class StudentsController(StudentService students) : ControllerBase
{
    /// <summary>Add a student to a student group (REQ-016).</summary>
    [HttpPost("api/student-groups/{groupId:int}/students")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(int groupId, AddStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await students.AddAsync(groupId, request.Name ?? string.Empty, request.Class, cancellationToken);

        // The members are listed with their group, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, StudentResponse.From(student));
    }

    /// <summary>Move a student to another student group (REQ-018).</summary>
    [HttpPut("api/students/{id:int}/group")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<StudentResponse> Move(int id, MoveStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await students.MoveAsync(id, request.StudentGroupId, cancellationToken);
        return StudentResponse.From(student);
    }

    /// <summary>Remove a student (REQ-019).</summary>
    [HttpDelete("api/students/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        await students.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}

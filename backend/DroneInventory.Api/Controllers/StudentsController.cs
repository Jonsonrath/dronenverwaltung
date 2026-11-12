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
}

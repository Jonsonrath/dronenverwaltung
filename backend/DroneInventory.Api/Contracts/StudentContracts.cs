using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/student-groups/{groupId}/students. Checked by the domain (REQ-016).</summary>
public record AddStudentRequest(string? Name, string? Class);

/// <summary>A student as returned by the API; Class is empty if not given.</summary>
public record StudentResponse(int Id, string Name, string Class, int StudentGroupId)
{
    public static StudentResponse From(Student student) =>
        new(student.Id, student.Name, student.Class, student.StudentGroupId);
}

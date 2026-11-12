using System.ComponentModel.DataAnnotations;
using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/student-groups. Name is checked by the domain (REQ-003).</summary>
public record CreateStudentGroupRequest(string? Name);

/// <summary>Body of PUT /api/student-groups/{id}. Name is checked by the domain (REQ-011).</summary>
public record RenameStudentGroupRequest(string? Name);

/// <summary>Body of PUT /api/student-groups/{id}/responsible (REQ-020).</summary>
public record SetResponsibleStudentRequest([Required] int? StudentId);

/// <summary>A student group as returned by the API; ResponsibleStudentId is null if none (REQ-020).</summary>
public record StudentGroupResponse(int Id, string Name, int? ResponsibleStudentId = null)
{
    public static StudentGroupResponse From(StudentGroup group) => new(group.Id, group.Name, group.ResponsibleStudentId);
}

/// <summary>A student group with its members, ordered by name, as listed by GET /api/student-groups (REQ-017).</summary>
public record StudentGroupListResponse(
    int Id, string Name, IReadOnlyList<StudentResponse> Students, int? ResponsibleStudentId = null)
{
    public static StudentGroupListResponse From(StudentGroup group, IEnumerable<Student> students) =>
        new(group.Id, group.Name, students.Select(StudentResponse.From).ToList(), group.ResponsibleStudentId);
}

using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/student-groups. Name is checked by the domain (REQ-003).</summary>
public record CreateStudentGroupRequest(string? Name);

/// <summary>A student group as returned by the API.</summary>
public record StudentGroupResponse(int Id, string Name)
{
    public static StudentGroupResponse From(StudentGroup group) => new(group.Id, group.Name);
}

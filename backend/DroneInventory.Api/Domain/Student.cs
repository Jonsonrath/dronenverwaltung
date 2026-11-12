namespace DroneInventory.Api.Domain;

// A member of exactly one StudentGroup (REQ-016). An entity of its own, not part of the
// StudentGroup aggregate, because it can be moved to another group (REQ-018).
public class Student
{
    public int Id { get; private set; }

    /// <summary>Display name in its original spelling, trimmed.</summary>
    public string Name { get; private set; }

    /// <summary>Trimmed, upper-case name; unique within the group (design.md §1).</summary>
    public string NormalizedName { get; private set; }

    /// <summary>Optional free text such as "4AHIF"; empty if not given.</summary>
    public string Class { get; private set; }

    public int StudentGroupId { get; private set; }

    /// <summary>
    /// Creates a student of the given group. Throws ValidationException for an empty name.
    /// Uniqueness within the group is checked by the service.
    /// </summary>
    public Student(int studentGroupId, string name, string? @class)
    {
        Name = EntityName.Require(name, "eines Schülers");
        NormalizedName = EntityName.Normalize(name);
        Class = @class?.Trim() ?? string.Empty;
        StudentGroupId = studentGroupId;
    }

    /// <summary>Moves the student to another group (REQ-018). Uniqueness is checked by the service.</summary>
    public void MoveTo(int studentGroupId) => StudentGroupId = studentGroupId;
}

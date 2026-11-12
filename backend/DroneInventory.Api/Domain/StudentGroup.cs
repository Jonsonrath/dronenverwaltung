namespace DroneInventory.Api.Domain;

// A group of students that loans drones and is responsible for them (REQ-003, REQ-011).
public class StudentGroup
{
    public int Id { get; private set; }

    /// <summary>Display name in its original spelling, trimmed.</summary>
    public string Name { get; private set; }

    /// <summary>Trimmed, upper-case name used for the unique index (design.md §4b).</summary>
    public string NormalizedName { get; private set; }

    /// <summary>
    /// The responsible Student of this group (REQ-020); null if none. Always a member of the group:
    /// cleared when the student is moved, and by the database (Set Null) when the student is removed.
    /// </summary>
    public int? ResponsibleStudentId { get; private set; }

    public StudentGroup(string name)
    {
        Name = EntityName.Require(name, "einer Schülergruppe");
        NormalizedName = EntityName.Normalize(name);
    }

    /// <summary>
    /// Changes the name (REQ-011). The same rule as for a new group: throws ValidationException
    /// if the name is empty or whitespace. Uniqueness is checked by the service.
    /// </summary>
    public void Rename(string name)
    {
        Name = EntityName.Require(name, "einer Schülergruppe");
        NormalizedName = EntityName.Normalize(name);
    }

    /// <summary>
    /// Marks a student of this group as responsible, replacing the previous one (REQ-020).
    /// Throws ValidationException if the student belongs to another group; nothing changes then.
    /// </summary>
    public void SetResponsible(Student student)
    {
        if (student.StudentGroupId != Id)
        {
            throw new ValidationException($"\"{student.Name}\" gehört nicht zur Gruppe \"{Name}\".");
        }

        ResponsibleStudentId = student.Id;
    }

    /// <summary>The group has no responsible student afterwards (REQ-020).</summary>
    public void ClearResponsible() => ResponsibleStudentId = null;
}

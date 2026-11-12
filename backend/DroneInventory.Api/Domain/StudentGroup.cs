namespace DroneInventory.Api.Domain;

// A group of students that loans drones and is responsible for them (REQ-003, REQ-011).
public class StudentGroup
{
    public int Id { get; private set; }

    /// <summary>Display name in its original spelling, trimmed.</summary>
    public string Name { get; private set; }

    /// <summary>Trimmed, upper-case name used for the unique index (design.md §4b).</summary>
    public string NormalizedName { get; private set; }

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
}

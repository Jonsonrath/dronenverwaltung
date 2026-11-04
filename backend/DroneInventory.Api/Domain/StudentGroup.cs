namespace DroneInventory.Api.Domain;

// A group of students that loans drones and is responsible for them (REQ-003).
public class StudentGroup
{
    public int Id { get; private set; }

    /// <summary>Display name in its original spelling, trimmed.</summary>
    public string Name { get; private set; }

    /// <summary>Trimmed, upper-case name used for the unique index (design.md §4b).</summary>
    public string NormalizedName { get; private set; }

    public StudentGroup(string name)
    {
        Name = EntityName.Require(name, "student group");
        NormalizedName = EntityName.Normalize(name);
    }
}

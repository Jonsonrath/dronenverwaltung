namespace DroneInventory.Api.Domain;

// An aircraft that can be loaned (REQ-001) and edited (REQ-013). Its availability is not stored here;
// it is derived from the active Loan (design.md §4).
public class Drone
{
    public int Id { get; private set; }

    /// <summary>Display name in its original spelling, trimmed.</summary>
    public string Name { get; private set; }

    /// <summary>Trimmed, upper-case name used for the unique index (design.md §4b).</summary>
    public string NormalizedName { get; private set; }

    public string Description { get; private set; }

    public Drone(string name, string? description)
    {
        Name = EntityName.Require(name, "einer Drohne");
        NormalizedName = EntityName.Normalize(name);
        Description = description?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Changes name and description (REQ-013) with the same rules as for a new drone:
    /// throws ValidationException for an empty name. Uniqueness is checked by the service.
    /// </summary>
    public void Edit(string name, string? description)
    {
        Name = EntityName.Require(name, "einer Drohne");
        NormalizedName = EntityName.Normalize(name);
        Description = description?.Trim() ?? string.Empty;
    }
}

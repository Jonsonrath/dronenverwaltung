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

    /// <summary>
    /// Reason of the reported damage (REQ-014); null when the drone is not damaged.
    /// No separate flag, so the two can never disagree (support-damaged-drones design §1).
    /// </summary>
    public string? DamageReason { get; private set; }

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

    /// <summary>
    /// Marks the drone as damaged (REQ-014). A new reason replaces the old one.
    /// Throws ValidationException for an empty reason; the drone stays unchanged then.
    /// </summary>
    public void ReportDamage(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("Der Grund für den Defekt darf nicht leer sein.");
        }

        DamageReason = reason.Trim();
    }

    /// <summary>
    /// Marks the drone as repaired (REQ-015) and removes the reason.
    /// Throws ConflictException if the drone is not damaged.
    /// </summary>
    public void Repair()
    {
        if (DamageReason is null)
        {
            throw new ConflictException($"Die Drohne \"{Name}\" ist nicht als defekt gemeldet.");
        }

        DamageReason = null;
    }
}

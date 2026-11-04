namespace DroneInventory.Api.Domain;

// Shared rules for the unique names of Drone and StudentGroup (design.md §4b).
public static class EntityName
{
    /// <summary>Trimmed name; throws ValidationException if it is empty or whitespace.</summary>
    public static string Require(string name, string entity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException($"The name of a {entity} must not be empty.");
        }

        return name.Trim();
    }

    /// <summary>
    /// Trim and upper-case with invariant culture, so "Übungsdrohne" and " ÜBUNGSDROHNE "
    /// give the same value (unlike SQLite NOCASE, this also covers umlauts).
    /// </summary>
    public static string Normalize(string name) => name.Trim().ToUpperInvariant();
}

using System.Text.Json.Serialization;

namespace DroneInventory.Api.Domain;

/// <summary>
/// Availability of a drone (REQ-002). Never stored: it is derived from the loans and the
/// damage of the drone by <see cref="DroneStatusRules.Derive"/> (design.md §4,
/// support-damaged-drones design §2).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DroneStatus>))]
public enum DroneStatus
{
    Available,
    Loaned,
    Damaged,
}

/// <summary>The one place where the drone status is derived (MODIFIED REQ-002).</summary>
public static class DroneStatusRules
{
    /// <summary>
    /// Loaned while the drone has an active loan; otherwise Damaged if a damage is reported;
    /// otherwise Available.
    /// </summary>
    public static DroneStatus Derive(bool hasActiveLoan, string? damageReason) =>
        hasActiveLoan ? DroneStatus.Loaned
        : damageReason is not null ? DroneStatus.Damaged
        : DroneStatus.Available;
}

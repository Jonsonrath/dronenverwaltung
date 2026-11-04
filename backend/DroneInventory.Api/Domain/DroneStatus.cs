using System.Text.Json.Serialization;

namespace DroneInventory.Api.Domain;

/// <summary>
/// Availability of a drone (REQ-002). Never stored: it is derived from the loans –
/// Loaned while the drone has an active loan, otherwise Available (design.md §4).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DroneStatus>))]
public enum DroneStatus
{
    Available,
    Loaned,
}

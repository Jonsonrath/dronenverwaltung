using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/drones. Name and the optional SSID are checked by the domain (REQ-001, REQ-024).</summary>
public record CreateDroneRequest(string? Name, string? Description, string? Ssid = null);

/// <summary>Body of PUT /api/drones/{id}. Name and the optional SSID are checked by the domain (REQ-013, REQ-024).</summary>
public record UpdateDroneRequest(string? Name, string? Description, string? Ssid = null);

/// <summary>Body of POST /api/drones/{id}/damage. Reason is checked by the domain (REQ-014).</summary>
public record ReportDamageRequest(string? Reason);

/// <summary>
/// A drone as returned by the API, with its derived status, for a damaged drone the damage reason
/// (REQ-002; null when not damaged) and its SSID (REQ-024; null if none).
/// </summary>
public record DroneResponse(int Id, string Name, string Description, DroneStatus Status, string? DamageReason = null, string? Ssid = null)
{
    public static DroneResponse From(Drone drone, DroneStatus status) =>
        new(drone.Id, drone.Name, drone.Description, status, drone.DamageReason, drone.Ssid);
}

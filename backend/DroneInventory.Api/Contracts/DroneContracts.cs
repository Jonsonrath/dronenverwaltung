using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/drones. Name and the optional SSID are checked by the domain (REQ-001, REQ-024).</summary>
public record CreateDroneRequest(string? Name, string? Description, string? Ssid = null);

/// <summary>Body of PUT /api/drones/{id}. Name and the optional SSID are checked by the domain (REQ-013, REQ-024).</summary>
public record UpdateDroneRequest(string? Name, string? Description, string? Ssid = null);

/// <summary>A drone as returned by the API, with its derived status (REQ-002) and its SSID, null if none (REQ-024).</summary>
public record DroneResponse(int Id, string Name, string Description, DroneStatus Status, string? Ssid = null)
{
    public static DroneResponse From(Drone drone, DroneStatus status) =>
        new(drone.Id, drone.Name, drone.Description, status, drone.Ssid);
}

using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/drones. Name is checked by the domain (REQ-001).</summary>
public record CreateDroneRequest(string? Name, string? Description);

/// <summary>Body of PUT /api/drones/{id}. Name is checked by the domain (REQ-013).</summary>
public record UpdateDroneRequest(string? Name, string? Description);

/// <summary>A drone as returned by the API, with its derived status (REQ-002).</summary>
public record DroneResponse(int Id, string Name, string Description, DroneStatus Status)
{
    public static DroneResponse From(Drone drone, DroneStatus status) =>
        new(drone.Id, drone.Name, drone.Description, status);
}

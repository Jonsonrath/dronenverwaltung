using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

/// <summary>Body of POST /api/drones. Name is checked by the domain (REQ-001).</summary>
public record CreateDroneRequest(string? Name, string? Description);

/// <summary>A drone as returned by the API. The derived status is added in task 4.7.</summary>
public record DroneResponse(int Id, string Name, string Description)
{
    public static DroneResponse From(Drone drone) => new(drone.Id, drone.Name, drone.Description);
}

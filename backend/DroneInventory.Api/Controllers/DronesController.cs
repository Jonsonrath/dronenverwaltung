using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/drones")]
public class DronesController(DroneService drones) : ControllerBase
{
    /// <summary>List all drones with their derived status, ordered by name (REQ-002).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DroneResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<DroneResponse>> List(CancellationToken cancellationToken)
    {
        var all = await drones.ListAsync(cancellationToken);
        return all.Select(d => DroneResponse.From(d.Drone, d.Status)).ToList();
    }

    /// <summary>Create a drone (REQ-001).</summary>
    [HttpPost]
    [ProducesResponseType<DroneResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        var drone = await drones.CreateAsync(request.Name ?? string.Empty, request.Description, cancellationToken: cancellationToken);

        // A new drone has no loans yet, so its derived status is Available (REQ-001).
        // No single-drone endpoint exists, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, DroneResponse.From(drone, DroneStatus.Available));
    }

    /// <summary>Edit name and description of a drone (REQ-013).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<DroneResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<DroneResponse> Update(int id, UpdateDroneRequest request, CancellationToken cancellationToken)
    {
        var (drone, status) = await drones.UpdateAsync(id, request.Name ?? string.Empty, request.Description, cancellationToken: cancellationToken);
        return DroneResponse.From(drone, status);
    }
}

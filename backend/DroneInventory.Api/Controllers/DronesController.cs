using DroneInventory.Api.Contracts;
using DroneInventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Controllers;

[ApiController]
[Route("api/drones")]
public class DronesController(DroneService drones) : ControllerBase
{
    /// <summary>Create a drone (REQ-001).</summary>
    [HttpPost]
    [ProducesResponseType<DroneResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        var drone = await drones.CreateAsync(request.Name ?? string.Empty, request.Description, cancellationToken);

        // No single-drone endpoint exists, so the response has no Location header.
        return StatusCode(StatusCodes.Status201Created, DroneResponse.From(drone));
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using DomainValidationException = DroneInventory.Api.Domain.ValidationException;
using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Errors;

// Test-only endpoints that raise each kind of error, so the mapping can be
// checked through the real HTTP pipeline.
[ApiController]
[Route("test/errors")]
public class ErrorTestController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult NotFound_() => throw new NotFoundException("Drone 42 does not exist.");

    [HttpGet("conflict")]
    public IActionResult Conflict_() => throw new ConflictException("Drone is already loaned.");

    [HttpGet("validation")]
    public IActionResult Validation() => throw new DomainValidationException("Name must not be empty.");

    [HttpGet("unexpected")]
    public IActionResult Unexpected() => throw new InvalidOperationException("Bug");

    [HttpPost("model")]
    public IActionResult Model(TestRequest request) => Ok();

    public record TestRequest([Required] string? Name);
}

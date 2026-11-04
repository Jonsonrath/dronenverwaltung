using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Spec drone: "Create drone (REQ-001)" via POST /api/drones (task 2.2) and
// "List drones with availability (REQ-002)" via GET /api/drones (task 2.3).
// Every test gets its own app and database.
public class DronesApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DronesApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_001_CreateDrone_Returns201WithIdNameAndDescription()
    {
        var response = await _client.PostAsJsonAsync("/api/drones",
            new { name = "TELLO-07", description = "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var drone = await response.Content.ReadFromJsonAsync<DroneResponse>();
        Assert.True(drone!.Id > 0);
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt", drone.Description);
        Assert.Equal(1, CountDrones());
    }

    [Fact]
    public async Task REQ_001_CreateDroneWithoutDescription_StoresEmptyDescription()
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-08" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var drone = await response.Content.ReadFromJsonAsync<DroneResponse>();
        Assert.Equal(string.Empty, drone!.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_001_EmptyName_Returns422AndCreatesNoDrone(string? name)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal(0, CountDrones());
    }

    [Theory]
    [InlineData("TELLO-07", "tello-07")]
    [InlineData("TELLO-07", "  TELLO-07 ")]
    [InlineData("Übungsdrohne", "ÜBUNGSDROHNE")]
    public async Task REQ_001_DuplicateName_Returns409AndCreatesNoSecondDrone(string existing, string duplicate)
    {
        await _client.PostAsJsonAsync("/api/drones", new { name = existing });

        var response = await _client.PostAsJsonAsync("/api/drones", new { name = duplicate });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal(1, CountDrones());
    }

    [Fact]
    public async Task REQ_002_NoDrones_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/drones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var drones = await response.Content.ReadFromJsonAsync<List<DroneResponse>>();
        Assert.Empty(drones!);
    }

    [Fact]
    public async Task REQ_002_ListsAllDronesWithIdNameAndDescriptionOrderedByName()
    {
        await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07", description = "SSID: TELLO-9FA21B" });
        await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-02" });

        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones");

        Assert.Collection(drones!,
            first =>
            {
                Assert.Equal("TELLO-02", first.Name);
                Assert.Equal(string.Empty, first.Description);
            },
            second =>
            {
                Assert.True(second.Id > 0);
                Assert.Equal("TELLO-07", second.Name);
                Assert.Equal("SSID: TELLO-9FA21B", second.Description);
            });
    }

    private int CountDrones()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Drones.Count();
    }
}

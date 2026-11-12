using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change drone-ssid, spec drone: "Drone SSID (REQ-024)" via POST, PUT and GET /api/drones.
// Every test gets its own app and database.
public class DroneSsidApiTests : IDisposable
{
    // Drone responses contain an enum that the API writes as a string.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string DuplicateMessage =
        "Eine Drohne mit der SSID \"tello-9fa21b\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).";

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DroneSsidApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_024_CreateDroneWithSsid_ListsTrimmedSsid()
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07", ssid = " TELLO-9FA21B " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("TELLO-9FA21B", (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!.Ssid);
        Assert.Equal("TELLO-9FA21B", Assert.Single(await Drones()).Ssid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task REQ_024_DroneWithoutSsid_IsListedWithoutSsid(string? ssid)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-08", ssid });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(Assert.Single(await Drones()).Ssid);
    }

    [Fact]
    public async Task REQ_024_DroneWithoutSsidField_IsListedWithSsidNull()
    {
        await CreateDrone("TELLO-08", null);

        var json = await _client.GetStringAsync("/api/drones");

        Assert.Contains("\"ssid\":null", json);
    }

    [Fact]
    public async Task REQ_024_SeveralDronesWithoutSsid_AreAllowed()
    {
        await CreateDrone("TELLO-08", null);

        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-09" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var drones = await Drones();
        Assert.Equal(["TELLO-08", "TELLO-09"], drones.Select(d => d.Name));
        Assert.All(drones, d => Assert.Null(d.Ssid));
    }

    [Fact]
    public async Task REQ_024_CreateWithSsidOfAnotherDrone_Returns409()
    {
        await CreateDrone("TELLO-07", "TELLO-9FA21B");

        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-08", ssid = "tello-9fa21b" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal((409, DuplicateMessage), (problem.Status, problem.Detail));
        Assert.Equal(["TELLO-07"], (await Drones()).Select(d => d.Name));
    }

    [Fact]
    public async Task REQ_024_EditWithSsidOfAnotherDrone_Returns409AndKeepsTheSsid()
    {
        await CreateDrone("TELLO-07", "TELLO-9FA21B");
        var other = await CreateDrone("TELLO-08", null);

        var response = await _client.PutAsJsonAsync($"/api/drones/{other.Id}", new { name = "TELLO-08", ssid = "tello-9fa21b" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal((409, DuplicateMessage), (problem.Status, problem.Detail));
        Assert.Equal(["TELLO-9FA21B", null], (await Drones()).Select(d => d.Ssid));
    }

    [Fact]
    public async Task REQ_024_ChangeTheSsid_Returns200AndListsIt()
    {
        var drone = await CreateDrone("TELLO-07", null);

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}", new { name = "TELLO-07", ssid = "TELLO-9FA21B" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("TELLO-9FA21B", (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!.Ssid);
        Assert.Equal("TELLO-9FA21B", Assert.Single(await Drones()).Ssid);
    }

    [Fact]
    public async Task REQ_024_EditKeepingTheOwnSsid_Returns200()
    {
        var drone = await CreateDrone("TELLO-07", "TELLO-9FA21B");

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}",
            new { name = "TELLO-07", description = "neu", ssid = "tello-9fa21b" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<DroneResponse> CreateDrone(string name, string? ssid)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name, ssid });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!;
    }

    private async Task<List<DroneResponse>> Drones() =>
        (await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones", JsonOptions))!;
}

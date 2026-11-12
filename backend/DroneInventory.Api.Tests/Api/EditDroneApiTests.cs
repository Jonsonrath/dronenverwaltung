using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change edit-drones, spec drone: "Edit drone (REQ-013)" via PUT /api/drones/{id}.
// Every test gets its own app and database.
public class EditDroneApiTests : IDisposable
{
    // Drone and loan responses contain enums that the API writes as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public EditDroneApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_013_RenameDrone_Returns200AndKeepsId()
    {
        var drone = await CreateDrone("TELO-07", "rechter Propellerschutz beschädigt");

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}",
            new { name = "TELLO-07", description = "rechter Propellerschutz beschädigt" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        Assert.Equal(new DroneResponse(drone.Id, "TELLO-07", "rechter Propellerschutz beschädigt", DroneStatus.Available, null), edited);
        Assert.Equal(["TELLO-07"], (await Drones()).Select(d => d.Name));
    }

    [Fact]
    public async Task REQ_013_ChangeDescription_KeepsName()
    {
        var drone = await CreateDrone("TELLO-07", "rechter Propellerschutz beschädigt");

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}",
            new { name = "TELLO-07", description = "Propellerschutz erneuert" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listed = Assert.Single(await Drones());
        Assert.Equal(("TELLO-07", "Propellerschutz erneuert"), (listed.Name, listed.Description));
    }

    [Fact]
    public async Task REQ_013_LoansShowTheNewName_AndDroneStaysLoaned()
    {
        var drone = await CreateDrone("TELO-07", null);
        await LoanToNewGroup(drone.Id);

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}", new { name = "TELLO-07" });

        var edited = await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        Assert.Equal(DroneStatus.Loaned, edited!.Status);
        var loans = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);
        Assert.Equal("TELLO-07", Assert.Single(loans!).DroneName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_013_EmptyName_Returns422AndKeepsTheName(string? name)
    {
        var drone = await CreateDrone("TELLO-07", null);

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}", new { name });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(422, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        Assert.Equal(["TELLO-07"], (await Drones()).Select(d => d.Name));
    }

    [Theory]
    [InlineData("TELLO-02", " tello-02 ")]
    [InlineData("Übungsdrohne", "ÜBUNGSDROHNE")]
    public async Task REQ_013_NameOfAnotherDrone_Returns409AndKeepsTheName(string other, string newName)
    {
        await CreateDrone(other, null);
        var drone = await CreateDrone("TELLO-07", null);

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}", new { name = newName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        Assert.Contains("TELLO-07", (await Drones()).Select(d => d.Name));
    }

    [Fact]
    public async Task REQ_013_ChangingOnlyTheSpellingOfTheOwnName_Returns200()
    {
        var drone = await CreateDrone("tello-07", null);

        var response = await _client.PutAsJsonAsync($"/api/drones/{drone.Id}", new { name = "TELLO-07" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["TELLO-07"], (await Drones()).Select(d => d.Name));
    }

    [Fact]
    public async Task REQ_013_UnknownDrone_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/drones/999", new { name = "TELLO-07" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
    }

    private async Task<DroneResponse> CreateDrone(string name, string? description)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name, description });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!;
    }

    private async Task LoanToNewGroup(int droneId)
    {
        var groupResponse = await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" });
        var group = (await groupResponse.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
        (await _client.PostAsJsonAsync("/api/loans", new { droneId, studentGroupId = group.Id })).EnsureSuccessStatusCode();
    }

    private async Task<List<DroneResponse>> Drones() =>
        (await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones", JsonOptions))!;
}

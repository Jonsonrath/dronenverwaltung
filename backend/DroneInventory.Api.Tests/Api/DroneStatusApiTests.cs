using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Api;

// Task 4.7, spec drone: the derived status in "Create drone (REQ-001)" and
// "List drones with availability (REQ-002)". Also closes the drone-status checks that
// earlier tasks deferred: "TELLO-07 is Loaned" / "stays Available" (REQ-004, REQ-006)
// and "becomes available again" (REQ-008).
public class DroneStatusApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DroneStatusApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_001_NewDroneIsAvailable()
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07" });

        var created = await response.Content.ReadFromJsonAsync<DroneResponse>();
        Assert.Equal(DroneStatus.Available, created!.Status);
        Assert.Equal(DroneStatus.Available, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_002_ShowStatusOfDrones()
    {
        var loaned = await CreateDrone("TELLO-02");
        await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 1");
        await LoanDrone(loaned.Id, group.Id);

        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones");

        Assert.Collection(drones!,
            d => Assert.Equal(("TELLO-02", DroneStatus.Loaned), (d.Name, d.Status)),
            d => Assert.Equal(("TELLO-07", DroneStatus.Available), (d.Name, d.Status)));
    }

    [Fact]
    public async Task REQ_002_DroneWithOnlyReturnedLoansIsAvailable()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");
        var loanId = await LoanDrone(drone.Id, group.Id);
        Assert.Equal(DroneStatus.Loaned, await StatusOf("TELLO-07"));

        await _client.PostAsync($"/api/loans/{loanId}/return", null);

        // REQ-008: "TELLO-07 becomes available again".
        Assert.Equal(DroneStatus.Available, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_004_LoanedDroneIsLoaned()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        await LoanDrone(drone.Id, group.Id);

        Assert.Equal(DroneStatus.Loaned, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_004_RejectedLoanForUnknownGroupLeavesDroneAvailable()
    {
        var drone = await CreateDrone("TELLO-07");

        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(DroneStatus.Available, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_006_RejectedLoanWithNegativeQuantityLeavesDroneAvailable()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone.Id, studentGroupId = group.Id, batteries = -1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(DroneStatus.Available, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_002_StatusIsSentAsText()
    {
        await CreateDrone("TELLO-07");

        using var json = JsonDocument.Parse(await _client.GetStringAsync("/api/drones"));

        Assert.Equal("Available", json.RootElement[0].GetProperty("status").GetString());
    }

    private async Task<DroneStatus> StatusOf(string droneName)
    {
        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones");
        return drones!.Single(d => d.Name == droneName).Status;
    }

    private async Task<DroneResponse> CreateDrone(string name) =>
        (await (await _client.PostAsJsonAsync("/api/drones", new { name }))
            .Content.ReadFromJsonAsync<DroneResponse>())!;

    private async Task<StudentGroupResponse> CreateGroup(string name) =>
        (await (await _client.PostAsJsonAsync("/api/student-groups", new { name }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>())!;

    private async Task<int> LoanDrone(int droneId, int groupId)
    {
        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId, studentGroupId = groupId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}

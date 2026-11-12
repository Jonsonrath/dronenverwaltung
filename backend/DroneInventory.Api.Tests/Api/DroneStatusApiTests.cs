using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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

    // Change support-damaged-drones, task 1.2: MODIFIED "List drones with availability (REQ-002)".
    // The damage is written directly into the database here; the damage endpoints follow in task 1.3.

    [Fact]
    public async Task REQ_002_DamagedDrone()
    {
        var drone = await CreateDrone("TELLO-11");
        await MarkDamaged(drone.Id, "Propeller gebrochen");

        var listed = (await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones"))!.Single();

        Assert.Equal((DroneStatus.Damaged, "Propeller gebrochen"), (listed.Status, listed.DamageReason));
    }

    [Fact]
    public async Task REQ_002_LoanedDroneThatIsDamaged()
    {
        var drone = await CreateDrone("TELLO-02");
        var group = await CreateGroup("Group 3");
        await LoanDrone(drone.Id, group.Id);
        await MarkDamaged(drone.Id, "Kamera ausgefallen");

        var listed = (await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones"))!.Single();

        Assert.Equal((DroneStatus.Loaned, "Kamera ausgefallen"), (listed.Status, listed.DamageReason));
    }

    [Fact]
    public async Task REQ_002_UndamagedDroneHasNoDamageReasonAndDamagedIsSentAsText()
    {
        await CreateDrone("TELLO-07");
        var damaged = await CreateDrone("TELLO-11");
        await MarkDamaged(damaged.Id, "Propeller gebrochen");

        using var json = JsonDocument.Parse(await _client.GetStringAsync("/api/drones"));

        Assert.Equal(JsonValueKind.Null, json.RootElement[0].GetProperty("damageReason").ValueKind);
        Assert.Equal("Damaged", json.RootElement[1].GetProperty("status").GetString());
        Assert.Equal("Propeller gebrochen", json.RootElement[1].GetProperty("damageReason").GetString());
    }

    private async Task MarkDamaged(int droneId, string reason)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        (await db.Drones.FindAsync(droneId))!.ReportDamage(reason);
        await db.SaveChangesAsync();
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

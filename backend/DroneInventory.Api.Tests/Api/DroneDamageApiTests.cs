using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change support-damaged-drones, task 1.3, spec drone: "Report drone damage (REQ-014)" via
// POST /api/drones/{id}/damage and "Repair drone (REQ-015)" via DELETE /api/drones/{id}/damage.
// Every test gets its own app and database.
public class DroneDamageApiTests : IDisposable
{
    // Drone and loan responses contain enums that the API writes as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DroneDamageApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // REQ-014

    [Fact]
    public async Task REQ_014_MarkAnAvailableDroneAsDamaged()
    {
        var drone = await CreateDrone("TELLO-07");

        var response = await ReportDamage(drone.Id, "Propeller gebrochen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var damaged = await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        Assert.Equal(new DroneResponse(drone.Id, "TELLO-07", string.Empty, DroneStatus.Damaged, "Propeller gebrochen"), damaged);
        Assert.Equal((DroneStatus.Damaged, "Propeller gebrochen"), await StateOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_014_MarkALoanedDroneAsDamaged_LoanIsUnchanged()
    {
        var drone = await CreateDrone("TELLO-07");
        await LoanToGroup3(drone.Id);
        var loanBefore = await ActiveLoanJson();

        var response = await ReportDamage(drone.Id, "Kamera ausgefallen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((DroneStatus.Loaned, "Kamera ausgefallen"), await StateOf("TELLO-07"));
        Assert.Equal(loanBefore, await ActiveLoanJson());
    }

    [Fact]
    public async Task REQ_014_DamagedDroneAfterItsReturnIsDamaged()
    {
        var drone = await CreateDrone("TELLO-07");
        var loanId = await LoanToGroup3(drone.Id);
        await ReportDamage(drone.Id, "Kamera ausgefallen");

        (await _client.PostAsync($"/api/loans/{loanId}/return", null)).EnsureSuccessStatusCode();

        Assert.Equal((DroneStatus.Damaged, "Kamera ausgefallen"), await StateOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_014_NewReasonReplacesTheOldOne()
    {
        var drone = await CreateDrone("TELLO-07");
        await ReportDamage(drone.Id, "Propeller gebrochen");

        var response = await ReportDamage(drone.Id, "Akkufach locker");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((DroneStatus.Damaged, "Akkufach locker"), await StateOf("TELLO-07"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_014_EmptyReason_Returns422AndDroneStaysAvailable(string? reason)
    {
        var drone = await CreateDrone("TELLO-07");

        var response = await ReportDamage(drone.Id, reason);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Der Grund für den Defekt darf nicht leer sein.", problem!.Detail);
        Assert.Equal((DroneStatus.Available, (string?)null), await StateOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_014_UnknownDrone_Returns404()
    {
        var response = await ReportDamage(999, "Propeller gebrochen");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
    }

    // REQ-015

    [Fact]
    public async Task REQ_015_RepairADamagedDrone()
    {
        var drone = await CreateDrone("TELLO-07");
        await ReportDamage(drone.Id, "Propeller gebrochen");

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}/damage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var repaired = await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        Assert.Equal((DroneStatus.Available, (string?)null), (repaired!.Status, repaired.DamageReason));
        Assert.Equal((DroneStatus.Available, (string?)null), await StateOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_015_RepairALoanedDamagedDrone()
    {
        var drone = await CreateDrone("TELLO-07");
        await LoanToGroup3(drone.Id);
        await ReportDamage(drone.Id, "Kamera ausgefallen");

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}/damage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((DroneStatus.Loaned, (string?)null), await StateOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_015_DroneNotDamaged_Returns409()
    {
        var drone = await CreateDrone("TELLO-07");

        var response = await _client.DeleteAsync($"/api/drones/{drone.Id}/damage");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Drohne \"TELLO-07\" ist nicht als defekt gemeldet.", problem!.Detail);
    }

    [Fact]
    public async Task REQ_015_UnknownDrone_Returns404()
    {
        var response = await _client.DeleteAsync("/api/drones/999/damage");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
    }

    private Task<HttpResponseMessage> ReportDamage(int droneId, string? reason) =>
        _client.PostAsJsonAsync($"/api/drones/{droneId}/damage", new { reason });

    private async Task<DroneResponse> CreateDrone(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!;
    }

    private async Task<int> LoanToGroup3(int droneId)
    {
        var groupResponse = await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" });
        var group = (await groupResponse.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId, studentGroupId = group.Id, batteries = 4, propellers = 8 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<string> ActiveLoanJson() => await _client.GetStringAsync("/api/loans?status=Active");

    private async Task<(DroneStatus Status, string? DamageReason)> StateOf(string droneName)
    {
        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones", JsonOptions);
        var drone = drones!.Single(d => d.Name == droneName);
        return (drone.Status, drone.DamageReason);
    }
}

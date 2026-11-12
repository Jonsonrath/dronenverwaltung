using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change support-damaged-drones, task 1.4, spec loan: MODIFIED "Loan drone (REQ-004)" –
// a damaged drone is rejected with 409 by POST /api/loans.
public class DamagedDroneLoanApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DamagedDroneLoanApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_004_DamagedDroneCannotBeLoaned_Returns409()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");
        await ReportDamage(drone.Id, "Propeller gebrochen");

        var response = await Loan(drone.Id, group.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Drohne \"TELLO-07\" ist defekt: Propeller gebrochen", problem!.Detail);
        Assert.Empty((await _client.GetFromJsonAsync<List<JsonElement>>("/api/loans?status=Active"))!);
        Assert.Equal(DroneStatus.Damaged, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_004_RepairedDroneCanBeLoaned()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");
        await ReportDamage(drone.Id, "Propeller gebrochen");
        (await _client.DeleteAsync($"/api/drones/{drone.Id}/damage")).EnsureSuccessStatusCode();

        var response = await Loan(drone.Id, group.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.Equal(("TELLO-07", "Group 3", LoanStatus.Active), (loan!.DroneName, loan.StudentGroupName, loan.Status));
        Assert.Equal(DroneStatus.Loaned, await StatusOf("TELLO-07"));
    }

    [Fact]
    public async Task REQ_004_UnknownGroupIsCheckedBeforeTheDamage_Returns404()
    {
        var drone = await CreateDrone("TELLO-07");
        await ReportDamage(drone.Id, "Propeller gebrochen");

        var response = await Loan(drone.Id, 999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task REQ_004_LoanedDamagedDrone_AnswersWithTheDamage()
    {
        // Order of the answers (design §4): damaged 409 before already loaned 409.
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");
        (await Loan(drone.Id, group.Id)).EnsureSuccessStatusCode();
        await ReportDamage(drone.Id, "Kamera ausgefallen");

        var response = await Loan(drone.Id, group.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Drohne \"TELLO-07\" ist defekt: Kamera ausgefallen", problem!.Detail);
    }

    private Task<HttpResponseMessage> Loan(int droneId, int groupId) =>
        _client.PostAsJsonAsync("/api/loans", new { droneId, studentGroupId = groupId, batteries = 2, propellers = 4 });

    private async Task ReportDamage(int droneId, string reason) =>
        (await _client.PostAsJsonAsync($"/api/drones/{droneId}/damage", new { reason })).EnsureSuccessStatusCode();

    private async Task<DroneResponse> CreateDrone(string name) =>
        (await (await _client.PostAsJsonAsync("/api/drones", new { name }))
            .Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!;

    private async Task<StudentGroupResponse> CreateGroup(string name) =>
        (await (await _client.PostAsJsonAsync("/api/student-groups", new { name }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>())!;

    private async Task<DroneStatus> StatusOf(string droneName)
    {
        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones", JsonOptions);
        return drones!.Single(d => d.Name == droneName).Status;
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change damage-report-on-return, task 1.2: "Report damage on return (REQ-023)" via the
// optional body of POST /api/loans/{id}/return, the loan history and the drone list.
public class DamageReportOnReturnApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public DamageReportOnReturnApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_023_ReturnWithDamageReport_HistoryShowsReportAndDroneIsDamaged()
    {
        var loan = await LoanTello07ToGroup3();

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return",
            new { damageReport = "Propeller gebrochen" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.Equal("Propeller gebrochen", returned!.DamageReport);

        var entry = await SingleHistoryEntry();
        Assert.Equal("Propeller gebrochen", entry.DamageReport);
        var drone = await Tello07();
        Assert.Equal(DroneStatus.Damaged, drone.Status);
        Assert.Equal("Propeller gebrochen", drone.DamageReason);
    }

    [Fact]
    public async Task REQ_023_ReturnWithoutDamageReport_NoReportAndDroneAvailable()
    {
        var loan = await LoanTello07ToGroup3();

        var response = await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertNoReportAndDroneAvailable();
    }

    [Fact]
    public async Task REQ_023_ReturnWithQuantitiesButWithoutDamageReport_NoReportAndDroneAvailable()
    {
        var loan = await LoanTello07ToGroup3();

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { batteries = 3, propellers = 8 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertNoReportAndDroneAvailable();
    }

    [Fact]
    public async Task REQ_023_WhitespaceOnlyReport_NoReportAndDroneAvailable()
    {
        var loan = await LoanTello07ToGroup3();

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { damageReport = "   " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertNoReportAndDroneAvailable();
    }

    [Fact]
    public async Task REQ_023_DamagedDroneCannotBeLoanedAfterTheReturn_Returns409()
    {
        var loan = await LoanTello07ToGroup3();
        await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { damageReport = "Propeller gebrochen" });
        var group1 = await (await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 1" }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>();

        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = loan.DroneId, studentGroupId = group1!.Id, batteries = 0, propellers = 0 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Drohne \"TELLO-07\" ist defekt: Propeller gebrochen", problem!.Detail);
    }

    [Fact]
    public async Task REQ_023_ActiveLoanHasNoDamageReport()
    {
        await LoanTello07ToGroup3();

        var active = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);

        Assert.Null(Assert.Single(active!).DamageReport);
    }

    private async Task AssertNoReportAndDroneAvailable()
    {
        Assert.Null((await SingleHistoryEntry()).DamageReport);
        var drone = await Tello07();
        Assert.Equal(DroneStatus.Available, drone.Status);
        Assert.Null(drone.DamageReason);
    }

    private async Task<DroneResponse> Tello07()
    {
        var drones = await _client.GetFromJsonAsync<List<DroneResponse>>("/api/drones", JsonOptions);
        return Assert.Single(drones!);
    }

    private async Task<LoanResponse> SingleHistoryEntry()
    {
        var history = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);
        return Assert.Single(history!);
    }

    private async Task<LoanResponse> LoanTello07ToGroup3()
    {
        var drone = await (await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07" }))
            .Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        var group = await (await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>();
        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone!.Id, studentGroupId = group!.Id, batteries = 4, propellers = 8 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }
}

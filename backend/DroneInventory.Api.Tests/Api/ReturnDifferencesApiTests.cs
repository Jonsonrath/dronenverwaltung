using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change return-differences, task 1.2: "Record returned accessories (REQ-022)" via
// POST /api/loans/{id}/return with an optional body and the loan history.
public class ReturnDifferencesApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReturnDifferencesApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_022_CompleteReturn_WithoutBodyEverythingIsReturned()
    {
        var loan = await LoanTello07ToGroup3(4, 8);

        var response = await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = await SingleHistoryEntry();
        Assert.Equal(4, entry.ReturnedBatteries);
        Assert.Equal(8, entry.ReturnedPropellers);
        // Nothing missing: loaned minus returned.
        Assert.Equal(0, entry.Batteries - entry.ReturnedBatteries);
        Assert.Equal(0, entry.Propellers - entry.ReturnedPropellers);
    }

    [Fact]
    public async Task REQ_022_CompleteReturn_EmptyJsonBodyEverythingIsReturned()
    {
        var loan = await LoanTello07ToGroup3(4, 8);

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.Equal(4, returned!.ReturnedBatteries);
        Assert.Equal(8, returned.ReturnedPropellers);
    }

    [Fact]
    public async Task REQ_022_MissingBattery_HistoryShowsOneBatteryMissing()
    {
        var loan = await LoanTello07ToGroup3(4, 8);

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { batteries = 3, propellers = 8 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.Equal(3, returned!.ReturnedBatteries);
        Assert.Equal(8, returned.ReturnedPropellers);

        var entry = await SingleHistoryEntry();
        Assert.Equal(4, entry.Batteries);
        Assert.Equal(3, entry.ReturnedBatteries);
        Assert.Equal(1, entry.Batteries - entry.ReturnedBatteries);
        Assert.Equal(0, entry.Propellers - entry.ReturnedPropellers);
    }

    [Fact]
    public async Task REQ_022_MoreReturnedThanLoaned_Returns422AndLoanStaysActive()
    {
        var loan = await LoanTello07ToGroup3(4, 8);

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { batteries = 5 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Anzahl der zurückgegebenen Akkus muss zwischen 0 und 4 liegen.", problem!.Detail);
        await AssertLoanStillActive(loan.Id);
    }

    [Fact]
    public async Task REQ_022_NegativeReturnedQuantity_Returns422AndLoanStaysActive()
    {
        var loan = await LoanTello07ToGroup3(4, 8);

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { propellers = -1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Die Anzahl der zurückgegebenen Propeller muss zwischen 0 und 8 liegen.", problem!.Detail);
        await AssertLoanStillActive(loan.Id);
    }

    [Fact]
    public async Task REQ_022_ActiveLoanHasNoReturnedQuantities()
    {
        await LoanTello07ToGroup3(4, 8);

        var active = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);

        var entry = Assert.Single(active!);
        Assert.Null(entry.ReturnedBatteries);
        Assert.Null(entry.ReturnedPropellers);
    }

    [Fact]
    public async Task REQ_008_AlreadyReturnedWithBody_Returns409()
    {
        var loan = await LoanTello07ToGroup3(4, 8);
        await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        var response = await _client.PostAsJsonAsync($"/api/loans/{loan.Id}/return", new { batteries = 3 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(4, (await SingleHistoryEntry()).ReturnedBatteries);
    }

    private async Task AssertLoanStillActive(int loanId)
    {
        var active = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);
        var entry = Assert.Single(active!);
        Assert.Equal(loanId, entry.Id);
        Assert.Equal(LoanStatus.Active, entry.Status);
        Assert.Null(entry.ReturnDate);
    }

    private async Task<LoanResponse> SingleHistoryEntry()
    {
        var history = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);
        return Assert.Single(history!);
    }

    private async Task<LoanResponse> LoanTello07ToGroup3(int batteries, int propellers)
    {
        var drone = await (await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07" }))
            .Content.ReadFromJsonAsync<DroneResponse>();
        var group = await (await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>();
        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone!.Id, studentGroupId = group!.Id, batteries, propellers });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }
}

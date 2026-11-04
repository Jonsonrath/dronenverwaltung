using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Task 4.5, spec loan: "Return loan (REQ-008)" and "Store return date (REQ-009)"
// via POST /api/loans/{id}/return. The loan is made on 19.08.2026 and returned 7 days later.
public class ReturnLoanApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReturnLoanApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_008_ReturnLoan_MarksReturnedAndDroneIsAvailableAgain()
    {
        var loan = await LoanDrone("TELLO-07", "Group 3", 4, 8);
        _factory.Time.Advance(TimeSpan.FromDays(7));

        var response = await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.Equal(LoanStatus.Returned, returned!.Status);

        // "TELLO-07 becomes available again": it has no active loan any more and can be
        // loaned again. The status field itself is added and checked in task 4.7.
        var active = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);
        Assert.Empty(active!);
        var group1 = await CreateGroup("Group 1");
        var again = await _client.PostAsJsonAsync("/api/loans", new { droneId = loan.DroneId, studentGroupId = group1.Id });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task REQ_009_ReturnDateIsStoredAndOtherFieldsAreUnchanged()
    {
        var loan = await LoanDrone("TELLO-07", "Group 3", 4, 8);
        _factory.Time.Advance(TimeSpan.FromDays(7));

        var returned = await (await _client.PostAsync($"/api/loans/{loan.Id}/return", null))
            .Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);

        Assert.Equal(ApiFactory.Start.UtcDateTime.AddDays(7), returned!.ReturnDate);
        Assert.Equal(loan.LoanDate, returned.LoanDate);
        Assert.Equal("Group 3", returned.StudentGroupName);
        Assert.Equal("TELLO-07", returned.DroneName);
        Assert.Equal(4, returned.Batteries);
        Assert.Equal(8, returned.Propellers);

        // Also stored that way, and the loan is kept (REQ-010).
        var stored = StoredLoan(loan.Id);
        Assert.Equal(LoanStatus.Returned, stored.Status);
        Assert.Equal(ApiFactory.Start.UtcDateTime.AddDays(7), stored.ReturnDate);
        Assert.Equal(loan.LoanDate, stored.LoanDate);
        Assert.Equal(4, stored.Batteries);
        Assert.Equal(8, stored.Propellers);
    }

    [Fact]
    public async Task REQ_008_LoanAlreadyReturned_Returns409AndKeepsReturnDate()
    {
        var loan = await LoanDrone("TELLO-07", "Group 3", 4, 8);
        _factory.Time.Advance(TimeSpan.FromDays(7));
        await _client.PostAsync($"/api/loans/{loan.Id}/return", null);
        _factory.Time.Advance(TimeSpan.FromDays(1));

        var response = await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        Assert.Equal(ApiFactory.Start.UtcDateTime.AddDays(7), StoredLoan(loan.Id).ReturnDate);
    }

    [Fact]
    public async Task REQ_008_UnknownLoan_Returns404()
    {
        var response = await _client.PostAsync("/api/loans/999/return", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
    }

    private async Task<StudentGroupResponse> CreateGroup(string name) =>
        (await (await _client.PostAsJsonAsync("/api/student-groups", new { name }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>())!;

    private async Task<LoanResponse> LoanDrone(string droneName, string groupName, int batteries, int propellers)
    {
        var drone = await (await _client.PostAsJsonAsync("/api/drones", new { name = droneName }))
            .Content.ReadFromJsonAsync<DroneResponse>();
        var group = await CreateGroup(groupName);
        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone!.Id, studentGroupId = group.Id, batteries, propellers });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }

    private Loan StoredLoan(int id)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>()
            .Loans.Include(l => l.Items).AsNoTracking().Single(l => l.Id == id);
    }
}

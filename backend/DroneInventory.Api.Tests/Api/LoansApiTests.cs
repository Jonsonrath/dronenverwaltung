using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Task 4.2, spec loan: "Loan drone (REQ-004)" and "Loan accessories (REQ-006)" via POST /api/loans.
// Every test gets its own app, database and fixed clock (19.08.2026).
public class LoansApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public LoansApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_004_LoanAvailableDrone_Returns201WithActiveLoan()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone.Id, studentGroupId = group.Id, batteries = 4, propellers = 8 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.True(loan!.Id > 0);
        Assert.Equal(drone.Id, loan.DroneId);
        Assert.Equal("TELLO-07", loan.DroneName);
        Assert.Equal(group.Id, loan.StudentGroupId);
        Assert.Equal("Group 3", loan.StudentGroupName);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(ApiFactory.Start.UtcDateTime, loan.LoanDate);
        Assert.Null(loan.ReturnDate);
    }

    [Fact]
    public async Task REQ_004_LoanResponseUsesTextStatusAndUtcDate()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = group.Id });

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Active", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("2026-08-19T08:00:00Z", json.RootElement.GetProperty("loanDate").GetString());
    }

    [Fact]
    public async Task REQ_006_LoanWithAccessories_ContainsBatteriesAndPropellers()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var loan = await CreateLoan(new { droneId = drone.Id, studentGroupId = group.Id, batteries = 4, propellers = 8 });

        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
        Assert.Equal(new[] { (LoanItemType.Battery, 4), (LoanItemType.Propeller, 8) }, StoredItems(loan.Id));
    }

    [Fact]
    public async Task REQ_006_LoanWithoutAccessories_ContainsZeroBatteriesAndPropellers()
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var loan = await CreateLoan(new { droneId = drone.Id, studentGroupId = group.Id });

        Assert.Equal(0, loan.Batteries);
        Assert.Equal(0, loan.Propellers);
        Assert.Equal(new[] { (LoanItemType.Battery, 0), (LoanItemType.Propeller, 0) }, StoredItems(loan.Id));
    }

    [Fact]
    public async Task REQ_004_UnknownDrone_Returns404AndCreatesNoLoan()
    {
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId = 999, studentGroupId = group.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        Assert.Equal(0, CountLoans());
    }

    [Fact]
    public async Task REQ_004_UnknownStudentGroup_Returns404AndCreatesNoLoan()
    {
        var drone = await CreateDrone("TELLO-07");

        var response = await _client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, CountLoans());
        // "TELLO-07 stays Available": see DroneStatusApiTests.REQ_004_RejectedLoanForUnknownGroupLeavesDroneAvailable.
    }

    [Theory]
    [InlineData(-1, 8)]
    [InlineData(4, -1)]
    public async Task REQ_006_NegativeQuantity_Returns422AndCreatesNoLoan(int batteries, int propellers)
    {
        var drone = await CreateDrone("TELLO-07");
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone.Id, studentGroupId = group.Id, batteries, propellers });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(422, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        Assert.Equal(0, CountLoans());
    }

    [Theory]
    [InlineData("{ \"studentGroupId\": 1 }")]
    [InlineData("{ \"droneId\": 1 }")]
    public async Task REQ_004_MissingDroneOrGroup_Returns422(string body)
    {
        var response = await _client.PostAsync("/api/loans",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<DroneResponse> CreateDrone(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name });
        return (await response.Content.ReadFromJsonAsync<DroneResponse>())!;
    }

    private async Task<StudentGroupResponse> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private async Task<LoanResponse> CreateLoan(object body)
    {
        var response = await _client.PostAsJsonAsync("/api/loans", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }

    private int CountLoans()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Loans.Count();
    }

    private (LoanItemType, int)[] StoredItems(int loanId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        return db.Loans.Include(l => l.Items).Single(l => l.Id == loanId).Items
            .OrderBy(i => i.Type)
            .Select(i => (i.Type, i.Quantity))
            .ToArray();
    }
}

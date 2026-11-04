using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Task 4.4, spec loan: Requirement "Show active loans (REQ-007)" via GET /api/loans?status=Active.
// Example from the assignment: Group 1 / TELLO-02 / 3 / 4 and Group 3 / TELLO-07 / 4 / 8, both on 19.08.2026.
public class ActiveLoansApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ActiveLoansApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_007_ShowsActiveLoansWithGroupDroneAccessoriesAndLoanDate()
    {
        await LoanDrone("TELLO-02", "Group 1", 3, 4);
        await LoanDrone("TELLO-07", "Group 3", 4, 8);

        var response = await _client.GetAsync("/api/loans?status=Active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loans = await response.Content.ReadFromJsonAsync<List<LoanResponse>>(JsonOptions);
        Assert.Collection(loans!,
            first => AssertLoan(first, "Group 1", "TELLO-02", 3, 4),
            second => AssertLoan(second, "Group 3", "TELLO-07", 4, 8));
    }

    [Fact]
    public async Task REQ_007_ReturnedLoansAreNotListed()
    {
        await LoanDrone("TELLO-02", "Group 1", 3, 4);
        var returned = await LoanDrone("TELLO-07", "Group 3", 4, 8);
        Return(returned.Id); // the return endpoint follows in task 4.5

        var loans = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);

        var loan = Assert.Single(loans!);
        Assert.Equal("TELLO-02", loan.DroneName);
    }

    [Fact]
    public async Task REQ_007_NoActiveLoans_ReturnsEmptyList()
    {
        var loans = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);

        Assert.Empty(loans!);
    }

    [Theory]
    [InlineData("/api/loans")]
    [InlineData("/api/loans?status=Lost")]
    public async Task MissingOrUnknownStatus_Returns422(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private static void AssertLoan(LoanResponse loan, string group, string drone, int batteries, int propellers)
    {
        Assert.Equal(group, loan.StudentGroupName);
        Assert.Equal(drone, loan.DroneName);
        Assert.Equal(batteries, loan.Batteries);
        Assert.Equal(propellers, loan.Propellers);
        Assert.Equal(new DateOnly(2026, 8, 19), DateOnly.FromDateTime(loan.LoanDate));
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Null(loan.ReturnDate);
    }

    private async Task<LoanResponse> LoanDrone(string droneName, string groupName, int batteries, int propellers)
    {
        var drone = await (await _client.PostAsJsonAsync("/api/drones", new { name = droneName }))
            .Content.ReadFromJsonAsync<DroneResponse>();
        var group = await (await _client.PostAsJsonAsync("/api/student-groups", new { name = groupName }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>();
        var response = await _client.PostAsJsonAsync("/api/loans",
            new { droneId = drone!.Id, studentGroupId = group!.Id, batteries, propellers });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }

    private void Return(int loanId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        db.Loans.Single(l => l.Id == loanId).Return(_factory.Time.GetUtcNow().UtcDateTime);
        db.SaveChanges();
    }
}

using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Task 4.3, spec loan: Requirement "No double loan (REQ-005)" via POST /api/loans.
public class NoDoubleLoanApiTests
{
    [Fact]
    public async Task REQ_005_LoanedDroneCannotBeLoanedAgain_Returns409AndKeepsExistingLoan()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var drone = await CreateDrone(client, "TELLO-07");
        var group3 = await CreateGroup(client, "Group 3");
        var group1 = await CreateGroup(client, "Group 1");
        var existing = await client.PostAsJsonAsync("/api/loans",
            new { droneId = drone.Id, studentGroupId = group3.Id, batteries = 4, propellers = 8 });
        Assert.Equal(HttpStatusCode.Created, existing.StatusCode);

        var response = await client.PostAsJsonAsync("/api/loans",
            new { droneId = drone.Id, studentGroupId = group1.Id, batteries = 1, propellers = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
        var loan = Assert.Single(Loans(factory));
        Assert.Equal(group3.Id, loan.StudentGroupId);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
    }

    [Fact]
    public async Task REQ_005_DroneCanBeLoanedAgainAfterReturn()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var drone = await CreateDrone(client, "TELLO-07");
        var group3 = await CreateGroup(client, "Group 3");
        var group1 = await CreateGroup(client, "Group 1");
        await client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = group3.Id });
        ReturnAllLoans(factory); // the return endpoint follows in task 4.5

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = group1.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var active = Assert.Single(Loans(factory), l => l.Status == LoanStatus.Active);
        Assert.Equal(group1.Id, active.StudentGroupId);
        Assert.Equal(2, Loans(factory).Count);
    }

    [Fact]
    public async Task REQ_005_SimultaneousLoanRequests_OneSucceedsOneConflicts()
    {
        // A database file instead of the shared in-memory connection, so the two
        // requests really run in parallel, each with its own connection.
        var file = Path.Combine(Path.GetTempPath(), $"drone-inventory-{Guid.NewGuid():N}.db");
        try
        {
            using var factory = ApiFactory.WithDatabaseFile(file);
            var client = factory.CreateClient();
            var group1 = await CreateGroup(client, "Group 1");
            var group3 = await CreateGroup(client, "Group 3");

            // Several rounds make it likely that both requests pass the check
            // before either saves, so the database index is exercised too.
            for (var round = 0; round < 5; round++)
            {
                var drone = await CreateDrone(client, $"TELLO-{round:00}");

                var responses = await Task.WhenAll(
                    client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = group1.Id }),
                    client.PostAsJsonAsync("/api/loans", new { droneId = drone.Id, studentGroupId = group3.Id }));

                Assert.Equal(
                    new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
                    responses.Select(r => r.StatusCode).Order());
                Assert.Single(Loans(factory), l => l.DroneId == drone.Id && l.Status == LoanStatus.Active);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(file);
        }
    }

    private static async Task<DroneResponse> CreateDrone(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/drones", new { name });
        return (await response.Content.ReadFromJsonAsync<DroneResponse>())!;
    }

    private static async Task<StudentGroupResponse> CreateGroup(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/student-groups", new { name });
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private static List<Loan> Loans(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>()
            .Loans.Include(l => l.Items).AsNoTracking().ToList();
    }

    private static void ReturnAllLoans(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        foreach (var loan in db.Loans.Where(l => l.Status == LoanStatus.Active))
        {
            loan.Return(factory.Time.GetUtcNow().UtcDateTime);
        }
        db.SaveChanges();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace DroneInventory.Api.Tests.Api;

// Task 4.6, spec loan: Requirement "Loan history (REQ-010)" via GET /api/loans?status=Returned.
public class LoanHistoryApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task REQ_010_ReturnedLoanStaysInHistoryWithLoanDateAndReturnDate()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var loan = await LoanDrone(client, "TELLO-07", "Group 3", 4, 8);
        factory.Time.Advance(TimeSpan.FromDays(7));
        await Return(client, loan.Id);

        var response = await client.GetAsync("/api/loans?status=Returned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<LoanResponse>>(JsonOptions);
        var entry = Assert.Single(history!);
        Assert.Equal("Group 3", entry.StudentGroupName);
        Assert.Equal("TELLO-07", entry.DroneName);
        Assert.Equal(4, entry.Batteries);
        Assert.Equal(8, entry.Propellers);
        Assert.Equal(ApiFactory.Start.UtcDateTime, entry.LoanDate);
        Assert.Equal(ApiFactory.Start.UtcDateTime.AddDays(7), entry.ReturnDate);
        Assert.Equal(LoanStatus.Returned, entry.Status);
    }

    [Fact]
    public async Task REQ_010_ActiveLoansAreNotInHistory()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        await LoanDrone(client, "TELLO-02", "Group 1", 3, 4);

        var history = await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);

        Assert.Empty(history!);
    }

    [Fact]
    public async Task REQ_010_HistoryShowsMostRecentReturnFirst()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var first = await LoanDrone(client, "TELLO-02", "Group 1", 3, 4);
        var second = await LoanDrone(client, "TELLO-07", "Group 3", 4, 8);
        factory.Time.Advance(TimeSpan.FromDays(1));
        await Return(client, first.Id);
        factory.Time.Advance(TimeSpan.FromDays(1));
        await Return(client, second.Id);

        var history = await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);

        Assert.Equal(["TELLO-07", "TELLO-02"], history!.Select(l => l.DroneName));
    }

    [Fact]
    public async Task REQ_010_ReturnedLoansCannotBeDeleted()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var loan = await LoanDrone(client, "TELLO-07", "Group 3", 4, 8);
        await Return(client, loan.Id);

        var response = await client.DeleteAsync($"/api/loans/{loan.Id}");

        // There is no delete endpoint for loans at all.
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Single((await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions))!);
    }

    [Fact]
    public async Task REQ_010_LoansSurviveARestart()
    {
        var file = Path.Combine(Path.GetTempPath(), $"drone-inventory-{Guid.NewGuid():N}.db");
        try
        {
            int loanId;
            using (var before = ApiFactory.WithDatabaseFile(file))
            {
                var client = before.CreateClient();
                var loan = await LoanDrone(client, "TELLO-07", "Group 3", 4, 8);
                before.Time.Advance(TimeSpan.FromDays(7));
                await Return(client, loan.Id);
                loanId = loan.Id;
            }

            // A new app instance on the same database file.
            SqliteConnection.ClearAllPools();
            using var after = ApiFactory.WithDatabaseFile(file);
            var history = await after.CreateClient()
                .GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);

            var entry = Assert.Single(history!);
            Assert.Equal(loanId, entry.Id);
            Assert.Equal("TELLO-07", entry.DroneName);
            Assert.Equal("Group 3", entry.StudentGroupName);
            Assert.Equal(4, entry.Batteries);
            Assert.Equal(8, entry.Propellers);
            Assert.Equal(ApiFactory.Start.UtcDateTime.AddDays(7), entry.ReturnDate);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(file);
        }
    }

    private static async Task<LoanResponse> LoanDrone(HttpClient client, string droneName, string groupName, int batteries, int propellers)
    {
        var drone = await (await client.PostAsJsonAsync("/api/drones", new { name = droneName }))
            .Content.ReadFromJsonAsync<DroneResponse>();
        var group = await (await client.PostAsJsonAsync("/api/student-groups", new { name = groupName }))
            .Content.ReadFromJsonAsync<StudentGroupResponse>();
        var response = await client.PostAsJsonAsync("/api/loans",
            new { droneId = drone!.Id, studentGroupId = group!.Id, batteries, propellers });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }

    private static async Task Return(HttpClient client, int loanId)
    {
        var response = await client.PostAsync($"/api/loans/{loanId}/return", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

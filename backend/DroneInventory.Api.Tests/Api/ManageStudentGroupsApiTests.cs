using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Change manage-student-groups, spec student-group: "Rename student group (REQ-011)" via
// PUT /api/student-groups/{id} and "Delete student group (REQ-012)" via DELETE /api/student-groups/{id}.
// Every test gets its own app and database.
public class ManageStudentGroupsApiTests : IDisposable
{
    // LoanResponse has an enum that the API writes as a string.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ManageStudentGroupsApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // ---- REQ-011 Rename ----

    [Fact]
    public async Task REQ_011_RenameStudentGroup_Returns200AndKeepsId()
    {
        var group = await CreateGroup("Grupe 3");

        var response = await _client.PutAsJsonAsync($"/api/student-groups/{group.Id}", new { name = "Group 3" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renamed = await response.Content.ReadFromJsonAsync<StudentGroupResponse>();
        Assert.Equal(new StudentGroupResponse(group.Id, "Group 3"), renamed);
        Assert.Equal(["Group 3"], await GroupNames());
    }

    [Fact]
    public async Task REQ_011_ActiveLoanShowsTheNewName()
    {
        var group = await CreateGroup("Grupe 3");
        await CreateLoan(group.Id, "TELLO-07");

        await _client.PutAsJsonAsync($"/api/student-groups/{group.Id}", new { name = "Group 3" });

        var loans = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);
        Assert.Equal("Group 3", Assert.Single(loans!).StudentGroupName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_011_EmptyName_Returns422AndKeepsTheName(string? name)
    {
        var group = await CreateGroup("Group 3");

        var response = await _client.PutAsJsonAsync($"/api/student-groups/{group.Id}", new { name });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal(["Group 3"], await GroupNames());
    }

    [Theory]
    [InlineData("Group 1", " group 1 ")]
    [InlineData("Gruppe Äpfel", "GRUPPE äpfel")]
    public async Task REQ_011_NameOfAnotherGroup_Returns409AndKeepsTheName(string other, string newName)
    {
        await CreateGroup(other);
        var group = await CreateGroup("Group 3");

        var response = await _client.PutAsJsonAsync($"/api/student-groups/{group.Id}", new { name = newName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Contains("Group 3", await GroupNames());
    }

    [Fact]
    public async Task REQ_011_ChangingOnlyTheSpellingOfTheOwnName_Returns200()
    {
        var group = await CreateGroup("group 3");

        var response = await _client.PutAsJsonAsync($"/api/student-groups/{group.Id}", new { name = "Group 3" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Group 3"], await GroupNames());
    }

    [Fact]
    public async Task REQ_011_UnknownStudentGroup_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/student-groups/999", new { name = "Group 3" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
    }

    // ---- REQ-012 Delete ----

    [Fact]
    public async Task REQ_012_DeleteStudentGroupWithoutLoans_Returns204AndGroupIsNoLongerListed()
    {
        await CreateGroup("Group 3");
        var unused = await CreateGroup("Group 86");

        var response = await _client.DeleteAsync($"/api/student-groups/{unused.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Group 3"], await GroupNames());
    }

    [Fact]
    public async Task REQ_012_StudentGroupWithActiveLoan_Returns409AndKeepsGroupAndLoan()
    {
        var group = await CreateGroup("Group 3");
        await CreateLoan(group.Id, "TELLO-07");

        var response = await _client.DeleteAsync($"/api/student-groups/{group.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal(["Group 3"], await GroupNames());
        Assert.Single((await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions))!);
    }

    [Fact]
    public async Task REQ_012_StudentGroupWithReturnedLoan_Returns409AndKeepsTheHistory()
    {
        var group = await CreateGroup("Group 3");
        var loan = await CreateLoan(group.Id, "TELLO-07");
        (await _client.PostAsync($"/api/loans/{loan.Id}/return", null)).EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"/api/student-groups/{group.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(["Group 3"], await GroupNames());
        var history = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Returned", JsonOptions);
        Assert.Equal(loan.Id, Assert.Single(history!).Id);
        Assert.Equal(1, CountLoans());
    }

    [Fact]
    public async Task REQ_012_StudentGroupWithStudents_Returns409AndKeepsGroupAndStudent()
    {
        await CreateGroup("Group 3");
        var group = await CreateGroup("Group 86");
        (await _client.PostAsJsonAsync($"/api/student-groups/{group.Id}/students", new { name = "Ben Huber" }))
            .EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"/api/student-groups/{group.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Die Schülergruppe \"Group 86\" hat noch Schüler und kann nicht gelöscht werden.", problem.Detail);
        var groups = await _client.GetFromJsonAsync<List<StudentGroupListResponse>>("/api/student-groups");
        var kept = Assert.Single(groups!, g => g.Name == "Group 86");
        Assert.Equal("Ben Huber", Assert.Single(kept.Students).Name);
    }

    [Fact]
    public async Task REQ_012_UnknownStudentGroup_Returns404()
    {
        var response = await _client.DeleteAsync("/api/student-groups/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
    }

    // ---- helpers ----

    private async Task<StudentGroupResponse> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private async Task<LoanResponse> CreateLoan(int studentGroupId, string droneName)
    {
        var droneResponse = await _client.PostAsJsonAsync("/api/drones", new { name = droneName });
        droneResponse.EnsureSuccessStatusCode();
        var drone = (await droneResponse.Content.ReadFromJsonAsync<DroneResponse>())!;

        var response = await _client.PostAsJsonAsync(
            "/api/loans", new { droneId = drone.Id, studentGroupId, batteries = 4, propellers = 8 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
    }

    private async Task<List<string>> GroupNames()
    {
        var groups = await _client.GetFromJsonAsync<List<StudentGroupResponse>>("/api/student-groups");
        return groups!.Select(g => g.Name).ToList();
    }

    private int CountLoans()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Loans.Count();
    }
}

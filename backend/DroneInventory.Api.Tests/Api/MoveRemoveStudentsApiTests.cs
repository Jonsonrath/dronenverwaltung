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

// Change student-group-members, spec student-group: "Move student (REQ-018)" via
// PUT /api/students/{id}/group and "Remove student (REQ-019)" via DELETE /api/students/{id}.
// Every test gets its own app and database.
public class MoveRemoveStudentsApiTests : IDisposable
{
    // LoanResponse has an enum that the API writes as a string.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public MoveRemoveStudentsApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // ---- REQ-018 Move ----

    [Fact]
    public async Task REQ_018_MoveStudent_Returns200AndStudentIsInTheNewGroupOnly()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        var anna = await AddStudent(group3.Id, "Anna Berger", "4AHIF");

        var response = await _client.PutAsJsonAsync($"/api/students/{anna.Id}/group", new { studentGroupId = group1.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moved = await response.Content.ReadFromJsonAsync<StudentResponse>();
        Assert.Equal(new StudentResponse(anna.Id, "Anna Berger", "4AHIF", group1.Id), moved);
        Assert.Equal(["Anna Berger"], await MemberNames("Group 1"));
        Assert.Empty(await MemberNames("Group 3"));
    }

    [Fact]
    public async Task REQ_018_NameAlreadyInTheTargetGroup_Returns409AndStudentStays()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        var anna = await AddStudent(group3.Id, "Anna Berger", null);
        await AddStudent(group1.Id, "anna berger", null);

        var response = await _client.PutAsJsonAsync($"/api/students/{anna.Id}/group", new { studentGroupId = group1.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal("In der Gruppe \"Group 1\" gibt es bereits einen Schüler \"Anna Berger\".", problem.Detail);
        Assert.Equal(["Anna Berger"], await MemberNames("Group 3"));
        Assert.Equal(["anna berger"], await MemberNames("Group 1"));
    }

    [Fact]
    public async Task REQ_018_UnknownStudent_Returns404()
    {
        var group1 = await CreateGroup("Group 1");

        var response = await _client.PutAsJsonAsync("/api/students/999/group", new { studentGroupId = group1.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Equal("Den Schüler 999 gibt es nicht.", problem.Detail);
    }

    [Fact]
    public async Task REQ_018_UnknownTargetGroup_Returns404AndStudentStays()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger", null);

        var response = await _client.PutAsJsonAsync($"/api/students/{anna.Id}/group", new { studentGroupId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Equal(["Anna Berger"], await MemberNames("Group 3"));
    }

    // ---- REQ-019 Remove ----

    [Fact]
    public async Task REQ_019_RemoveStudent_Returns204AndGroupIsListedWithoutTheStudent()
    {
        var group3 = await CreateGroup("Group 3");
        await AddStudent(group3.Id, "Anna Berger", null);
        var ben = await AddStudent(group3.Id, "Ben Huber", null);

        var response = await _client.DeleteAsync($"/api/students/{ben.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Anna Berger"], await MemberNames("Group 3"));
    }

    [Fact]
    public async Task REQ_019_RemoveStudent_DoesNotAffectLoans()
    {
        var group3 = await CreateGroup("Group 3");
        var ben = await AddStudent(group3.Id, "Ben Huber", null);
        var drone = await (await _client.PostAsJsonAsync("/api/drones", new { name = "TELLO-07" }))
            .Content.ReadFromJsonAsync<DroneResponse>(JsonOptions);
        (await _client.PostAsJsonAsync(
            "/api/loans", new { droneId = drone!.Id, studentGroupId = group3.Id, batteries = 4, propellers = 8 }))
            .EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"/api/students/{ben.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var loans = await _client.GetFromJsonAsync<List<LoanResponse>>("/api/loans?status=Active", JsonOptions);
        Assert.Equal("Group 3", Assert.Single(loans!).StudentGroupName);
    }

    [Fact]
    public async Task REQ_019_UnknownStudent_Returns404()
    {
        var response = await _client.DeleteAsync("/api/students/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Equal(0, CountStudents());
    }

    // ---- helpers ----

    private async Task<StudentGroupResponse> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private async Task<StudentResponse> AddStudent(int groupId, string name, string? @class)
    {
        var response = await _client.PostAsJsonAsync($"/api/student-groups/{groupId}/students", new { name, @class });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentResponse>())!;
    }

    private async Task<List<string>> MemberNames(string groupName)
    {
        var groups = await _client.GetFromJsonAsync<List<StudentGroupListResponse>>("/api/student-groups");
        return Assert.Single(groups!, g => g.Name == groupName).Students.Select(s => s.Name).ToList();
    }

    private int CountStudents()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Students.Count();
    }
}

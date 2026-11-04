using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Task 3.2, spec student-group: "Create student group (REQ-003)" and
// "List student groups (REQ-003)" via /api/student-groups.
// Every test gets its own app and database.
public class StudentGroupsApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public StudentGroupsApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_003_CreateStudentGroup_Returns201WithIdAndName()
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<StudentGroupResponse>();
        Assert.True(group!.Id > 0);
        Assert.Equal("Group 3", group.Name);
        Assert.Equal(1, CountGroups());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_003_EmptyName_Returns422AndCreatesNoGroup(string? name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal(0, CountGroups());
    }

    [Theory]
    [InlineData("Group 3", " group 3 ")]
    [InlineData("Gruppe Äpfel", "GRUPPE äpfel")]
    public async Task REQ_003_DuplicateName_Returns409AndCreatesNoSecondGroup(string existing, string duplicate)
    {
        await _client.PostAsJsonAsync("/api/student-groups", new { name = existing });

        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name = duplicate });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal(1, CountGroups());
    }

    [Fact]
    public async Task REQ_003_NoStudentGroups_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/student-groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var groups = await response.Content.ReadFromJsonAsync<List<StudentGroupResponse>>();
        Assert.Empty(groups!);
    }

    [Fact]
    public async Task REQ_003_ListsStudentGroupsWithIdAndNameOrderedByName()
    {
        await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" });
        await _client.PostAsJsonAsync("/api/student-groups", new { name = "Group 1" });

        var groups = await _client.GetFromJsonAsync<List<StudentGroupResponse>>("/api/student-groups");

        Assert.Equal(["Group 1", "Group 3"], groups!.Select(g => g.Name));
        Assert.All(groups!, g => Assert.True(g.Id > 0));
    }

    private int CountGroups()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().StudentGroups.Count();
    }
}

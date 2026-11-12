using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests.Api;

// Change student-group-members, spec student-group: "Add student to group (REQ-016)" via
// POST /api/student-groups/{groupId}/students and "List group members (REQ-017)" via GET /api/student-groups.
// Every test gets its own app and database.
public class StudentsApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public StudentsApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    // ---- REQ-016 Add ----

    [Fact]
    public async Task REQ_016_AddStudent_Returns201WithIdNameClassAndGroup()
    {
        var group = await CreateGroup("Group 3");

        var response = await AddStudent(group.Id, "Anna Berger", "4AHIF");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var student = (await response.Content.ReadFromJsonAsync<StudentResponse>())!;
        Assert.True(student.Id > 0);
        Assert.Equal(new StudentResponse(student.Id, "Anna Berger", "4AHIF", group.Id), student);
        var listed = Assert.Single((await ListGroup("Group 3")).Students);
        Assert.Equal(student, listed);
    }

    [Fact]
    public async Task REQ_016_AddStudentWithoutClass_StoresEmptyClass()
    {
        var group = await CreateGroup("Group 3");

        var response = await _client.PostAsJsonAsync($"/api/student-groups/{group.Id}/students", new { name = "Ben Huber" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(string.Empty, (await response.Content.ReadFromJsonAsync<StudentResponse>())!.Class);
        Assert.Equal(string.Empty, Assert.Single((await ListGroup("Group 3")).Students).Class);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task REQ_016_EmptyName_Returns422AndCreatesNoStudent(string? name)
    {
        var group = await CreateGroup("Group 3");

        var response = await AddStudent(group.Id, name, "4AHIF");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal("Der Name eines Schülers darf nicht leer sein.", problem.Detail);
        Assert.Equal(0, CountStudents());
    }

    [Theory]
    [InlineData("Anna Berger", " anna berger ")]
    [InlineData("Jürgen Öz", "JÜRGEN öz")]
    public async Task REQ_016_NameAlreadyInTheGroup_Returns409(string first, string second)
    {
        var group = await CreateGroup("Group 3");
        (await AddStudent(group.Id, first, null)).EnsureSuccessStatusCode();

        var response = await AddStudent(group.Id, second, null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Contains("Group 3", problem.Detail);
        Assert.Equal(1, CountStudents());
    }

    [Fact]
    public async Task REQ_016_SameNameInAnotherGroup_Returns201()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        (await AddStudent(group3.Id, "Anna Berger", null)).EnsureSuccessStatusCode();

        var response = await AddStudent(group1.Id, "Anna Berger", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Anna Berger", Assert.Single((await ListGroup("Group 1")).Students).Name);
    }

    [Fact]
    public async Task REQ_016_UnknownStudentGroup_Returns404()
    {
        var response = await AddStudent(999, "Anna Berger", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Equal(0, CountStudents());
    }

    // ---- REQ-017 List ----

    [Fact]
    public async Task REQ_017_GroupWithStudents_ListsThemOrderedByName()
    {
        var group = await CreateGroup("Group 3");
        (await AddStudent(group.Id, "Ben Huber", null)).EnsureSuccessStatusCode();
        (await AddStudent(group.Id, "Anna Berger", "4AHIF")).EnsureSuccessStatusCode();

        var listed = await ListGroup("Group 3");

        Assert.Equal(["Anna Berger", "Ben Huber"], listed.Students.Select(s => s.Name));
    }

    [Fact]
    public async Task REQ_017_GroupWithoutStudents_HasEmptyList()
    {
        var group3 = await CreateGroup("Group 3");
        (await AddStudent(group3.Id, "Anna Berger", null)).EnsureSuccessStatusCode();
        await CreateGroup("Group 86");

        var listed = await ListGroup("Group 86");

        Assert.NotNull(listed.Students);
        Assert.Empty(listed.Students);
    }

    // ---- helpers ----

    private async Task<StudentGroupResponse> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private Task<HttpResponseMessage> AddStudent(int groupId, string? name, string? @class) =>
        _client.PostAsJsonAsync($"/api/student-groups/{groupId}/students", new { name, @class });

    private async Task<StudentGroupListResponse> ListGroup(string name)
    {
        var groups = await _client.GetFromJsonAsync<List<StudentGroupListResponse>>("/api/student-groups");
        return Assert.Single(groups!, g => g.Name == name);
    }

    private int CountStudents()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Students.Count();
    }
}

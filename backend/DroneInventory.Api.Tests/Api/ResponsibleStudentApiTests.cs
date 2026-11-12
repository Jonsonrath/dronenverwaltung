using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Api;

// Change responsible-student, spec student-group: "Set responsible student (REQ-020)" via
// PUT / DELETE /api/student-groups/{id}/responsible. Every test gets its own app and database.
public class ResponsibleStudentApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ResponsibleStudentApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_020_MarkAResponsibleStudent_Returns200AndGroupIsListedWithIt()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        await AddStudent(group3.Id, "Ben Huber");

        var response = await SetResponsible(group3.Id, anna.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new StudentGroupResponse(group3.Id, "Group 3", anna.Id),
            await response.Content.ReadFromJsonAsync<StudentGroupResponse>());
        Assert.Equal(anna.Id, await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_ChangeTheResponsibleStudent_BenIsTheOnlyResponsible()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        var ben = await AddStudent(group3.Id, "Ben Huber");
        await SetResponsible(group3.Id, anna.Id);

        var response = await SetResponsible(group3.Id, ben.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ben.Id, await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_ClearTheResponsibleStudent_GroupHasNone()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        await SetResponsible(group3.Id, anna.Id);

        var response = await _client.DeleteAsync($"/api/student-groups/{group3.Id}/responsible");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_ClearWhenNoneIsSet_Returns200()
    {
        var group3 = await CreateGroup("Group 3");

        var response = await _client.DeleteAsync($"/api/student-groups/{group3.Id}/responsible");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_StudentOfAnotherGroup_Returns422AndResponsibleIsUnchanged()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        var clara = await AddStudent(group1.Id, "Clara Wolf");
        await SetResponsible(group3.Id, anna.Id);

        var response = await SetResponsible(group3.Id, clara.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Equal("\"Clara Wolf\" gehört nicht zur Gruppe \"Group 3\".", problem.Detail);
        Assert.Equal(anna.Id, await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_ResponsibleStudentMoved_GroupHasNone()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        await SetResponsible(group3.Id, anna.Id);

        var response = await _client.PutAsJsonAsync($"/api/students/{anna.Id}/group", new { studentGroupId = group1.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await ResponsibleOf("Group 3"));
        Assert.Null(await ResponsibleOf("Group 1"));
    }

    [Fact]
    public async Task REQ_020_ResponsibleStudentRemoved_GroupHasNone()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        await SetResponsible(group3.Id, anna.Id);

        var response = await _client.DeleteAsync($"/api/students/{anna.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_MovingAnotherStudent_KeepsTheResponsibleStudent()
    {
        var group3 = await CreateGroup("Group 3");
        var group1 = await CreateGroup("Group 1");
        var anna = await AddStudent(group3.Id, "Anna Berger");
        var ben = await AddStudent(group3.Id, "Ben Huber");
        await SetResponsible(group3.Id, anna.Id);

        await _client.PutAsJsonAsync($"/api/students/{ben.Id}/group", new { studentGroupId = group1.Id });

        Assert.Equal(anna.Id, await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_UnknownStudentGroup_Returns404()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3.Id, "Anna Berger");

        var set = await SetResponsible(999, anna.Id);
        var clear = await _client.DeleteAsync("/api/student-groups/999/responsible");

        Assert.Equal(HttpStatusCode.NotFound, set.StatusCode);
        Assert.Equal("Die Schülergruppe 999 gibt es nicht.", (await set.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
        Assert.Equal(HttpStatusCode.NotFound, clear.StatusCode);
    }

    [Fact]
    public async Task REQ_020_UnknownStudent_Returns404()
    {
        var group3 = await CreateGroup("Group 3");

        var response = await SetResponsible(group3.Id, 999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Den Schüler 999 gibt es nicht.", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
        Assert.Null(await ResponsibleOf("Group 3"));
    }

    [Fact]
    public async Task REQ_020_MissingStudentId_Returns422()
    {
        var group3 = await CreateGroup("Group 3");

        var response = await _client.PutAsJsonAsync($"/api/student-groups/{group3.Id}/responsible", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private Task<HttpResponseMessage> SetResponsible(int groupId, int studentId) =>
        _client.PutAsJsonAsync($"/api/student-groups/{groupId}/responsible", new { studentId });

    private async Task<StudentGroupResponse> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!;
    }

    private async Task<StudentResponse> AddStudent(int groupId, string name)
    {
        var response = await _client.PostAsJsonAsync($"/api/student-groups/{groupId}/students", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentResponse>())!;
    }

    private async Task<int?> ResponsibleOf(string groupName)
    {
        var groups = await _client.GetFromJsonAsync<List<StudentGroupListResponse>>("/api/student-groups");
        return Assert.Single(groups!, g => g.Name == groupName).ResponsibleStudentId;
    }
}

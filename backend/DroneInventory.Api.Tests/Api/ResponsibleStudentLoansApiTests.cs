using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Api;

// Change responsible-student, spec loan: "Responsible student on loans (REQ-021)".
// The loan copies the name of the responsible student at loan time. Every test gets its own app and database.
public class ResponsibleStudentLoansApiTests : IDisposable
{
    // LoanResponse has an enum that the API writes as a string.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ResponsibleStudentLoansApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task REQ_021_LoanStoresTheResponsibleStudent()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3, "Anna Berger");
        await SetResponsible(group3, anna);
        var tello07 = await CreateDrone("TELLO-07");

        var response = await CreateLoan(tello07, group3);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Anna Berger", (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!.ResponsibleStudentName);
        var active = Assert.Single(await List("Active"));
        Assert.Equal("Anna Berger", active.ResponsibleStudentName);
    }

    [Fact]
    public async Task REQ_021_GroupWithoutResponsibleStudent_LoanIsCreatedWithoutOne()
    {
        var group86 = await CreateGroup("Group 86");
        var tello02 = await CreateDrone("TELLO-02");

        var response = await CreateLoan(tello02, group86);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var active = Assert.Single(await List("Active"));
        Assert.Equal("TELLO-02", active.DroneName);
        Assert.Null(active.ResponsibleStudentName);
    }

    [Fact]
    public async Task REQ_021_HistoryKeepsTheNameAtLoanTime()
    {
        var group3 = await CreateGroup("Group 3");
        var anna = await AddStudent(group3, "Anna Berger");
        var ben = await AddStudent(group3, "Ben Huber");
        await SetResponsible(group3, anna);
        var tello07 = await CreateDrone("TELLO-07");
        var loan = (await (await CreateLoan(tello07, group3)).Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;

        await SetResponsible(group3, ben);
        (await _client.DeleteAsync($"/api/students/{anna}")).EnsureSuccessStatusCode();
        var returned = await _client.PostAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal("Anna Berger", (await returned.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!.ResponsibleStudentName);
        var history = Assert.Single(await List("Returned"));
        Assert.Equal("Anna Berger", history.ResponsibleStudentName);
    }

    private Task<HttpResponseMessage> CreateLoan(int droneId, int groupId) =>
        _client.PostAsJsonAsync("/api/loans", new { droneId, studentGroupId = groupId, batteries = 2, propellers = 4 });

    private async Task<List<LoanResponse>> List(string status) =>
        (await _client.GetFromJsonAsync<List<LoanResponse>>($"/api/loans?status={status}", JsonOptions))!;

    private async Task SetResponsible(int groupId, int studentId) =>
        (await _client.PutAsJsonAsync($"/api/student-groups/{groupId}/responsible", new { studentId }))
            .EnsureSuccessStatusCode();

    private async Task<int> CreateDrone(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/drones", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DroneResponse>(JsonOptions))!.Id;
    }

    private async Task<int> CreateGroup(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentGroupResponse>())!.Id;
    }

    private async Task<int> AddStudent(int groupId, string name)
    {
        var response = await _client.PostAsJsonAsync($"/api/student-groups/{groupId}/students", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentResponse>())!.Id;
    }
}

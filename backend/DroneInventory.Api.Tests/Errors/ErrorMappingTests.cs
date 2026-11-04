using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests.Errors;

// Task 1.3: domain exceptions become ProblemDetails with 404 / 409 / 422 (design.md §5).
public class ErrorMappingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "Drone 42 does not exist.")]
    [InlineData("conflict", HttpStatusCode.Conflict, "Drone is already loaned.")]
    [InlineData("validation", HttpStatusCode.UnprocessableEntity, "Name must not be empty.")]
    public async Task MapsDomainExceptionToProblemDetails(string path, HttpStatusCode status, string detail)
    {
        var response = await _client.GetAsync($"/test/errors/{path}");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal((int)status, problem!.Status);
        Assert.Equal(detail, problem.Detail);
    }

    [Fact]
    public async Task OtherExceptionsStayInternalServerError()
    {
        var response = await _client.GetAsync("/test/errors/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestBodyGives422()
    {
        var response = await _client.PostAsJsonAsync("/test/errors/model", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(422, problem!.Status);
        Assert.Contains("Name", problem.Errors.Keys);
    }
}

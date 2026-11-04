using System.Reflection;

namespace DroneInventory.Api.Tests;

// Placeholder for task 1.1: proves that the test project builds and references the API.
// Replaced by real requirement tests (REQ-001 ...) in later tasks.
public class ProjectSetupTests
{
    [Fact]
    public void TestProjectReferencesApiAssembly()
    {
        var api = Assembly.Load("DroneInventory.Api");

        Assert.Equal("DroneInventory.Api", api.GetName().Name);
    }
}

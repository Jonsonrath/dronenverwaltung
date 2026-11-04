using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Infrastructure;

public class TestDatabaseTests
{
    [Fact]
    public void CreatesMigratedInMemoryDatabase()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();

        Assert.True(context.Database.CanConnect());
        Assert.Empty(context.Database.GetPendingMigrations());
    }
}

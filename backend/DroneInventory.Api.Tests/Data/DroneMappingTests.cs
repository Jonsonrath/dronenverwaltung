using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Task 2.1: migration InitialCreate and the unique drone name (REQ-001).
public class DroneMappingTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void InitialCreateCreatesDronesTable()
    {
        using var context = _database.CreateContext();

        var tables = context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToList();

        Assert.Contains("Drones", tables);
        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_InitialCreate"));
    }

    [Fact]
    public void REQ_001_StoresAndLoadsDrone()
    {
        using (var context = _database.CreateContext())
        {
            context.Drones.Add(new Drone("TELLO-07", "SSID: TELLO-9FA21B"));
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        var drone = Assert.Single(readContext.Drones);
        Assert.True(drone.Id > 0);
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("SSID: TELLO-9FA21B", drone.Description);
    }

    [Theory]
    [InlineData("TELLO-07", "tello-07")]
    [InlineData("Übungsdrohne", "ÜBUNGSDROHNE")]
    public void REQ_001_DatabaseRejectsDuplicateName(string first, string second)
    {
        using var context = _database.CreateContext();
        context.Drones.Add(new Drone(first, null));
        context.SaveChanges();

        context.Drones.Add(new Drone(second, null));
        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsUniqueConstraintViolation());
    }
}

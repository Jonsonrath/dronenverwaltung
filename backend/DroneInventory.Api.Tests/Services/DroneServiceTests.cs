using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneInventory.Api.Tests.Services;

// Task 2.2: DroneService, including the race between duplicate check and save (design.md §4b).
public class DroneServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task REQ_001_CreatesDrone()
    {
        await using var context = _database.CreateContext();

        var drone = await new DroneService(context).CreateAsync("TELLO-07", "SSID: TELLO-9FA21B");

        Assert.True(drone.Id > 0);
        Assert.Single(context.Drones);
    }

    [Fact]
    public async Task REQ_001_DuplicateCreatedConcurrently_ThrowsConflict()
    {
        // Simulates a second request that saves the same name after our
        // duplicate check but before our SaveChanges.
        await using var context = _database.CreateContext(new InsertDuplicateBeforeSave(_database));

        await Assert.ThrowsAsync<ConflictException>(
            () => new DroneService(context).CreateAsync("TELLO-07", null));

        await using var check = _database.CreateContext();
        Assert.Single(check.Drones);
    }

    private sealed class InsertDuplicateBeforeSave(TestDatabase database) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            other.Drones.Add(new Drone("tello-07", null));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

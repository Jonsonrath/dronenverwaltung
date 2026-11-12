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

    // Change edit-drones: "Edit drone (REQ-013)".

    [Fact]
    public async Task REQ_013_RenameKeepsIdAndChangesDescription()
    {
        int id = await Seed("TELO-07", "rechter Propellerschutz beschädigt");
        await using var context = _database.CreateContext();

        var (drone, status) = await new DroneService(context).UpdateAsync(id, "TELLO-07", "Propellerschutz erneuert");

        Assert.Equal(id, drone.Id);
        Assert.Equal(("TELLO-07", "Propellerschutz erneuert"), (drone.Name, drone.Description));
        Assert.Equal(DroneStatus.Available, status);
        await using var check = _database.CreateContext();
        Assert.Equal("TELLO-07", Assert.Single(check.Drones).Name);
    }

    [Fact]
    public async Task REQ_013_LoanedDroneStaysLoaned()
    {
        int id = await Seed("TELO-07", null);
        await using (var setup = _database.CreateContext())
        {
            var group = new StudentGroup("Group 3");
            setup.StudentGroups.Add(group);
            setup.Loans.Add(new Loan(await setup.Drones.SingleAsync(), group, 4, 8, DateTime.UtcNow));
            await setup.SaveChangesAsync();
        }
        await using var context = _database.CreateContext();

        var (_, status) = await new DroneService(context).UpdateAsync(id, "TELLO-07", null);

        Assert.Equal(DroneStatus.Loaned, status);
    }

    [Fact]
    public async Task REQ_013_ChangingOnlyTheSpellingOfTheOwnNameIsAllowed()
    {
        int id = await Seed("tello-07", null);
        await using var context = _database.CreateContext();

        var (drone, _) = await new DroneService(context).UpdateAsync(id, "TELLO-07", null);

        Assert.Equal("TELLO-07", drone.Name);
    }

    [Fact]
    public async Task REQ_013_NameOfAnotherDrone_ThrowsConflictAndKeepsName()
    {
        await Seed("TELLO-02", null);
        int id = await Seed("TELLO-07", null);
        await using var context = _database.CreateContext();

        await Assert.ThrowsAsync<ConflictException>(() => new DroneService(context).UpdateAsync(id, " tello-02 ", null));

        await using var check = _database.CreateContext();
        Assert.Equal("TELLO-07", (await check.Drones.SingleAsync(d => d.Id == id)).Name);
    }

    [Fact]
    public async Task REQ_013_NameTakenConcurrently_ThrowsConflict()
    {
        int id = await Seed("TELLO-07", null);
        // Another request saves "tello-99" after our check but before our save.
        await using var context = _database.CreateContext(new InsertNameBeforeSave(_database, "tello-99"));

        await Assert.ThrowsAsync<ConflictException>(() => new DroneService(context).UpdateAsync(id, "TELLO-99", null));
    }

    [Fact]
    public async Task REQ_013_UnknownDrone_ThrowsNotFound()
    {
        await using var context = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() => new DroneService(context).UpdateAsync(999, "TELLO-07", null));
    }

    private async Task<int> Seed(string name, string? description)
    {
        await using var context = _database.CreateContext();
        var drone = new Drone(name, description);
        context.Drones.Add(drone);
        await context.SaveChangesAsync();
        return drone.Id;
    }

    private sealed class InsertNameBeforeSave(TestDatabase database, string name) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            other.Drones.Add(new Drone(name, null));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
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

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

    // Change drone-ssid: "Drone SSID (REQ-024)".

    [Fact]
    public async Task REQ_024_CreatesDroneWithSsid()
    {
        await using var context = _database.CreateContext();

        var drone = await new DroneService(context).CreateAsync("TELLO-07", null, " TELLO-9FA21B ");

        await using var check = _database.CreateContext();
        Assert.Equal("TELLO-9FA21B", (await check.Drones.SingleAsync(d => d.Id == drone.Id)).Ssid);
    }

    [Fact]
    public async Task REQ_024_SeveralDronesWithoutSsid()
    {
        await using var context = _database.CreateContext();
        var service = new DroneService(context);

        await service.CreateAsync("TELLO-08", null);
        await service.CreateAsync("TELLO-09", null, "");

        await using var check = _database.CreateContext();
        Assert.Equal(2, await check.Drones.CountAsync(d => d.Ssid == null));
    }

    [Fact]
    public async Task REQ_024_CreateWithSsidOfAnotherDrone_ThrowsConflict()
    {
        await Seed("TELLO-07", null, "TELLO-9FA21B");
        await using var context = _database.CreateContext();

        var error = await Assert.ThrowsAsync<ConflictException>(
            () => new DroneService(context).CreateAsync("TELLO-08", null, "tello-9fa21b"));

        Assert.Equal(
            "Eine Drohne mit der SSID \"tello-9fa21b\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).",
            error.Message);
        await using var check = _database.CreateContext();
        Assert.Single(check.Drones);
    }

    [Fact]
    public async Task REQ_024_EditWithSsidOfAnotherDrone_ThrowsConflictAndKeepsSsid()
    {
        await Seed("TELLO-07", null, "TELLO-9FA21B");
        int id = await Seed("TELLO-08", null);
        await using var context = _database.CreateContext();

        await Assert.ThrowsAsync<ConflictException>(
            () => new DroneService(context).UpdateAsync(id, "TELLO-08", null, "tello-9fa21b"));

        await using var check = _database.CreateContext();
        Assert.Null((await check.Drones.SingleAsync(d => d.Id == id)).Ssid);
    }

    [Fact]
    public async Task REQ_024_EditSetsSsidAndKeepsOwnSsid()
    {
        int id = await Seed("TELLO-07", null);
        await using var context = _database.CreateContext();
        var service = new DroneService(context);

        await service.UpdateAsync(id, "TELLO-07", null, "TELLO-9FA21B");
        // Saving again with the own SSID in another spelling is no conflict.
        var (drone, _) = await service.UpdateAsync(id, "TELLO-07", "neu", "tello-9fa21b");

        Assert.Equal("tello-9fa21b", drone.Ssid);
    }

    [Fact]
    public async Task REQ_024_SsidTakenConcurrently_ThrowsSsidConflict()
    {
        // Another request saves a drone with the same SSID after our check but before our save.
        await using var context = _database.CreateContext(new InsertDroneBeforeSave(_database, "TELLO-99", "tello-9fa21b"));

        var error = await Assert.ThrowsAsync<ConflictException>(
            () => new DroneService(context).CreateAsync("TELLO-07", null, "TELLO-9FA21B"));

        Assert.Contains("SSID", error.Message);
    }

    private sealed class InsertDroneBeforeSave(TestDatabase database, string name, string ssid) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            other.Drones.Add(new Drone(name, null, ssid));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private async Task<int> Seed(string name, string? description, string? ssid = null)
    {
        await using var context = _database.CreateContext();
        var drone = new Drone(name, description, ssid);
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

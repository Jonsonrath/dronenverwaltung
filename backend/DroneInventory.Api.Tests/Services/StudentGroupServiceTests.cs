using DroneInventory.Api.Domain;
using DroneInventory.Api.Services;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneInventory.Api.Tests.Services;

// Task 3.2: StudentGroupService, including the race between duplicate check and save (design.md §4b).
public class StudentGroupServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task REQ_003_DuplicateCreatedConcurrently_ThrowsConflict()
    {
        // Simulates a second request that saves the same name after our
        // duplicate check but before our SaveChanges.
        await using var context = _database.CreateContext(new InsertDuplicateBeforeSave(_database));

        await Assert.ThrowsAsync<ConflictException>(
            () => new StudentGroupService(context).CreateAsync("Group 3"));

        await using var check = _database.CreateContext();
        Assert.Single(check.StudentGroups);
    }

    // REQ-012: the delete must only become 409 when a loan really blocks it.

    [Fact]
    public async Task REQ_012_LoanCreatedConcurrently_ThrowsConflictAndKeepsGroup()
    {
        int groupId = await SeedGroup("Group 3");
        // Simulates a loan for the group that is saved after our check but before our delete.
        await using var context = _database.CreateContext(new InsertLoanBeforeSave(_database, groupId));

        await Assert.ThrowsAsync<ConflictException>(() => new StudentGroupService(context).DeleteAsync(groupId));

        await using var check = _database.CreateContext();
        Assert.Single(check.StudentGroups);
        Assert.Single(check.Loans);
    }

    [Fact]
    public async Task REQ_012_OtherDatabaseErrorOnDelete_IsPassedOnUnchanged()
    {
        int groupId = await SeedGroup("Group 86");
        var failure = new DbUpdateException("disk I/O error");
        await using var context = _database.CreateContext(new FailBeforeSave(failure));

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(
            () => new StudentGroupService(context).DeleteAsync(groupId));

        Assert.Same(failure, thrown);
    }

    private async Task<int> SeedGroup(string name)
    {
        await using var context = _database.CreateContext();
        var group = new StudentGroup(name);
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();
        return group.Id;
    }

    private sealed class InsertLoanBeforeSave(TestDatabase database, int groupId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            var group = await other.StudentGroups.SingleAsync(g => g.Id == groupId, cancellationToken);
            var drone = new Drone("TELLO-07", null);
            other.Drones.Add(drone);
            other.Loans.Add(new Loan(drone, group, 4, 8, DateTime.UtcNow));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class FailBeforeSave(DbUpdateException failure) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw failure;
    }

    private sealed class InsertDuplicateBeforeSave(TestDatabase database) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using var other = database.CreateContext();
            other.StudentGroups.Add(new StudentGroup("group 3"));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

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

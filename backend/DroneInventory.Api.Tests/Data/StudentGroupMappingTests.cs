using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Task 3.1: migration AddStudentGroups and the unique group name (REQ-003).
public class StudentGroupMappingTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void AddStudentGroupsCreatesStudentGroupsTable()
    {
        using var context = _database.CreateContext();

        var tables = context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToList();

        Assert.Contains("StudentGroups", tables);
        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_AddStudentGroups"));
    }

    [Fact]
    public void REQ_003_StoresAndLoadsStudentGroup()
    {
        using (var context = _database.CreateContext())
        {
            context.StudentGroups.Add(new StudentGroup("Group 3"));
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        var group = Assert.Single(readContext.StudentGroups);
        Assert.True(group.Id > 0);
        Assert.Equal("Group 3", group.Name);
    }

    [Theory]
    [InlineData("Group 3", " group 3 ")]
    [InlineData("Gruppe Äpfel", "GRUPPE äpfel")]
    public void REQ_003_DatabaseRejectsDuplicateName(string first, string second)
    {
        using var context = _database.CreateContext();
        context.StudentGroups.Add(new StudentGroup(first));
        context.SaveChanges();

        context.StudentGroups.Add(new StudentGroup(second));
        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsUniqueConstraintViolation());
    }

    [Fact]
    public void DroneAndStudentGroupMayHaveTheSameName()
    {
        // Uniqueness applies per entity type, not across drones and groups.
        using var context = _database.CreateContext();
        context.Drones.Add(new Drone("Alpha", null));
        context.StudentGroups.Add(new StudentGroup("Alpha"));

        context.SaveChanges();

        Assert.Single(context.StudentGroups);
    }
}

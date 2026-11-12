using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Change responsible-student, task 1.1: migration AddResponsibleStudent,
// StudentGroup.ResponsibleStudentId (Set Null, REQ-020) and Loan.ResponsibleStudentName (REQ-021).
public class ResponsibleStudentMappingTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void AddResponsibleStudentMigrationIsApplied()
    {
        using var context = _database.CreateContext();

        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_AddResponsibleStudent"));
    }

    [Fact]
    public void REQ_020_StoresAndLoadsTheResponsibleStudent()
    {
        var (groupId, annaId) = Group3WithAnna();
        using (var context = _database.CreateContext())
        {
            var group = context.StudentGroups.Single();
            group.SetResponsible(context.Students.Single());
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        Assert.Equal(annaId, readContext.StudentGroups.Single(g => g.Id == groupId).ResponsibleStudentId);
    }

    [Fact]
    public void REQ_020_RemovingTheResponsibleStudentClearsItInTheDatabase()
    {
        var (groupId, _) = Group3WithAnna();
        using (var context = _database.CreateContext())
        {
            context.StudentGroups.Single().SetResponsible(context.Students.Single());
            context.SaveChanges();
        }

        // A fresh context that does not track the group, like StudentService.RemoveAsync.
        using (var context = _database.CreateContext())
        {
            context.Students.Remove(context.Students.Single());
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        Assert.Null(readContext.StudentGroups.Single(g => g.Id == groupId).ResponsibleStudentId);
    }

    [Theory]
    [InlineData("Anna Berger")]
    [InlineData(null)]
    public void REQ_021_StoresAndLoadsTheResponsibleStudentName(string? name)
    {
        using (var context = _database.CreateContext())
        {
            var drone = new Drone("TELLO-07", null);
            var group = new StudentGroup("Group 3");
            context.Loans.Add(new Loan(drone, group, 2, 4, DateTime.UtcNow, name));
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        Assert.Equal(name, readContext.Loans.Single().ResponsibleStudentName);
    }

    private (int GroupId, int AnnaId) Group3WithAnna()
    {
        using var context = _database.CreateContext();
        var group = new StudentGroup("Group 3");
        context.StudentGroups.Add(group);
        context.SaveChanges();
        var anna = new Student(group.Id, "Anna Berger", null);
        context.Students.Add(anna);
        context.SaveChanges();
        return (group.Id, anna.Id);
    }
}

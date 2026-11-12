using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Data;

// Change student-group-members, task 1.1: migration AddStudents, unique name within a group (REQ-016)
// and the foreign key to StudentGroups with Restrict (REQ-012).
public class StudentMappingTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void AddStudentsCreatesStudentsTable()
    {
        using var context = _database.CreateContext();

        var tables = context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToList();

        Assert.Contains("Students", tables);
        Assert.Contains(context.Database.GetAppliedMigrations(), m => m.EndsWith("_AddStudents"));
    }

    [Fact]
    public void REQ_016_StoresAndLoadsStudent()
    {
        var groupId = AddGroup("Group 3");
        using (var context = _database.CreateContext())
        {
            context.Students.Add(new Student(groupId, "Anna Berger", "4AHIF"));
            context.SaveChanges();
        }

        using var readContext = _database.CreateContext();
        var student = Assert.Single(readContext.Students);
        Assert.True(student.Id > 0);
        Assert.Equal("Anna Berger", student.Name);
        Assert.Equal("4AHIF", student.Class);
        Assert.Equal(groupId, student.StudentGroupId);
    }

    [Theory]
    [InlineData("Anna Berger", " anna berger ")]
    [InlineData("Jürgen Öz", "JÜRGEN öz")]
    public void REQ_016_DatabaseRejectsDuplicateNameInTheSameGroup(string first, string second)
    {
        var groupId = AddGroup("Group 3");
        using var context = _database.CreateContext();
        context.Students.Add(new Student(groupId, first, null));
        context.SaveChanges();

        context.Students.Add(new Student(groupId, second, null));
        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsUniqueConstraintViolation());
    }

    [Fact]
    public void REQ_016_SameNameInAnotherGroupIsAllowed()
    {
        var group3 = AddGroup("Group 3");
        var group1 = AddGroup("Group 1");
        using var context = _database.CreateContext();
        context.Students.Add(new Student(group3, "Anna Berger", null));
        context.Students.Add(new Student(group1, "Anna Berger", null));

        context.SaveChanges();

        Assert.Equal(2, context.Students.Count());
    }

    [Fact]
    public void REQ_016_DatabaseRejectsUnknownGroup()
    {
        using var context = _database.CreateContext();
        context.Students.Add(new Student(999, "Anna Berger", null));

        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsForeignKeyViolation());
    }

    [Fact]
    public void REQ_012_DatabaseRejectsDeletingAGroupWithStudents()
    {
        var groupId = AddGroup("Group 86");
        using (var setup = _database.CreateContext())
        {
            setup.Students.Add(new Student(groupId, "Ben Huber", null));
            setup.SaveChanges();
        }

        // A fresh context that does not track the student, like the service later.
        using var context = _database.CreateContext();
        context.StudentGroups.Remove(context.StudentGroups.Single());
        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.True(error.IsForeignKeyViolation());
    }

    private int AddGroup(string name)
    {
        using var context = _database.CreateContext();
        var group = new StudentGroup(name);
        context.StudentGroups.Add(group);
        context.SaveChanges();
        return group.Id;
    }
}

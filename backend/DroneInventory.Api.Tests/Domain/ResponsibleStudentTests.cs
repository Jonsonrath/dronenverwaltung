using DroneInventory.Api.Domain;
using DroneInventory.Api.Tests.Infrastructure;

namespace DroneInventory.Api.Tests.Domain;

// Change responsible-student, task 1.1: StudentGroup.SetResponsible / ClearResponsible (REQ-020)
// and Loan.ResponsibleStudentName (REQ-021).
public class ResponsibleStudentTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void REQ_020_MarkAResponsibleStudent()
    {
        var (group3, anna, _) = Group3WithAnnaAndBen();

        group3.SetResponsible(anna);

        Assert.Equal(anna.Id, group3.ResponsibleStudentId);
    }

    [Fact]
    public void REQ_020_ChangeTheResponsibleStudent()
    {
        var (group3, anna, ben) = Group3WithAnnaAndBen();
        group3.SetResponsible(anna);

        group3.SetResponsible(ben);

        Assert.Equal(ben.Id, group3.ResponsibleStudentId);
    }

    [Fact]
    public void REQ_020_ClearTheResponsibleStudent()
    {
        var (group3, anna, _) = Group3WithAnnaAndBen();
        group3.SetResponsible(anna);

        group3.ClearResponsible();

        Assert.Null(group3.ResponsibleStudentId);
    }

    [Fact]
    public void REQ_020_StudentOfAnotherGroupRejected()
    {
        var (group3, anna, _) = Group3WithAnnaAndBen();
        group3.SetResponsible(anna);
        using var context = _database.CreateContext();
        var group1 = new StudentGroup("Group 1");
        context.StudentGroups.Add(group1);
        context.SaveChanges();
        var clara = new Student(group1.Id, "Clara Wolf", null);
        context.Students.Add(clara);
        context.SaveChanges();

        var error = Assert.Throws<ValidationException>(() => group3.SetResponsible(clara));

        Assert.Equal("\"Clara Wolf\" gehört nicht zur Gruppe \"Group 3\".", error.Message);
        Assert.Equal(anna.Id, group3.ResponsibleStudentId);
    }

    [Theory]
    [InlineData("Anna Berger")]
    [InlineData(null)]
    public void REQ_021_LoanKeepsTheResponsibleStudentName(string? name)
    {
        var loan = new Loan(new Drone("TELLO-07", null), new StudentGroup("Group 3"), 1, 2, DateTime.UtcNow, name);

        Assert.Equal(name, loan.ResponsibleStudentName);
    }

    private (StudentGroup Group, Student Anna, Student Ben) Group3WithAnnaAndBen()
    {
        using var context = _database.CreateContext();
        var group = new StudentGroup("Group 3");
        context.StudentGroups.Add(group);
        context.SaveChanges();
        var anna = new Student(group.Id, "Anna Berger", null);
        var ben = new Student(group.Id, "Ben Huber", null);
        context.Students.AddRange(anna, ben);
        context.SaveChanges();
        return (group, anna, ben);
    }
}

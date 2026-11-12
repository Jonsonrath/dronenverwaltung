using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Domain;

// Change student-group-members, spec student-group: "Add student to group (REQ-016)", "Move student (REQ-018)".
public class StudentTests
{
    [Fact]
    public void REQ_016_CreatesStudentWithNameClassAndGroup()
    {
        var student = new Student(3, " Anna Berger ", " 4AHIF ");

        Assert.Equal("Anna Berger", student.Name);
        Assert.Equal("4AHIF", student.Class);
        Assert.Equal(3, student.StudentGroupId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_016_StudentWithoutClassHasEmptyClass(string? @class)
    {
        Assert.Equal(string.Empty, new Student(3, "Ben Huber", @class).Class);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_016_RejectsEmptyName(string name)
    {
        var error = Assert.Throws<ValidationException>(() => new Student(3, name, null));

        Assert.Equal("Der Name eines Schülers darf nicht leer sein.", error.Message);
    }

    [Theory]
    [InlineData("Anna Berger", " anna berger ")]
    [InlineData("Jürgen Öz", "JÜRGEN öz")]
    public void REQ_016_SameNormalizedNameIgnoringCaseUmlautsAndSpaces(string first, string second)
    {
        Assert.Equal(new Student(3, first, null).NormalizedName, new Student(3, second, null).NormalizedName);
    }

    [Fact]
    public void REQ_018_MoveToChangesTheGroup()
    {
        var student = new Student(3, "Anna Berger", "4AHIF");

        student.MoveTo(1);

        Assert.Equal(1, student.StudentGroupId);
        Assert.Equal("Anna Berger", student.Name);
    }
}

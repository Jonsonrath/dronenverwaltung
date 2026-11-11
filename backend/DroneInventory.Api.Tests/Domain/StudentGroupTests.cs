using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Domain;

// Task 3.1, spec student-group: Requirement "Create student group (REQ-003)".
public class StudentGroupTests
{
    [Fact]
    public void REQ_003_CreatesStudentGroupWithName()
    {
        var group = new StudentGroup("Group 3");

        Assert.Equal("Group 3", group.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_003_RejectsEmptyName(string name)
    {
        Assert.Throws<ValidationException>(() => new StudentGroup(name));
    }

    [Fact]
    public void REQ_011_RenameChangesNameAndNormalizedName()
    {
        var group = new StudentGroup("Grupe 3");

        group.Rename("  Group 3 ");

        Assert.Equal("Group 3", group.Name);
        Assert.Equal("GROUP 3", group.NormalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_011_RenameRejectsEmptyNameAndKeepsOldName(string name)
    {
        var group = new StudentGroup("Group 3");

        Assert.Throws<ValidationException>(() => group.Rename(name));
        Assert.Equal("Group 3", group.Name);
    }

    [Fact]
    public void REQ_003_TrimsName()
    {
        Assert.Equal("Group 3", new StudentGroup("  Group 3 ").Name);
    }

    [Theory]
    [InlineData("Group 3", " group 3 ")]
    [InlineData("Gruppe Äpfel", "GRUPPE äpfel")]
    public void REQ_003_SameNormalizedNameIgnoringCaseUmlautsAndSpaces(string first, string second)
    {
        Assert.Equal(new StudentGroup(first).NormalizedName, new StudentGroup(second).NormalizedName);
    }
}

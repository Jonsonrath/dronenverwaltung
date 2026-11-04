using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests.Domain;

// Task 2.1, spec drone: Requirement "Create drone (REQ-001)".
public class DroneTests
{
    [Fact]
    public void REQ_001_CreatesDroneWithNameAndDescription()
    {
        var drone = new Drone("TELLO-07", "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt");

        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt", drone.Description);
    }

    [Fact]
    public void REQ_001_DescriptionIsOptional()
    {
        var drone = new Drone("TELLO-08", null);

        Assert.Equal(string.Empty, drone.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_001_RejectsEmptyName(string name)
    {
        Assert.Throws<ValidationException>(() => new Drone(name, null));
    }

    [Fact]
    public void REQ_001_TrimsName()
    {
        var drone = new Drone("  TELLO-07 ", null);

        Assert.Equal("TELLO-07", drone.Name);
    }

    [Theory]
    [InlineData("TELLO-07", "tello-07")]
    [InlineData("Übungsdrohne", "ÜBUNGSDROHNE")]
    [InlineData("Group Ärzte", " group ärzte ")]
    public void REQ_001_SameNormalizedNameIgnoringCaseUmlautsAndSpaces(string first, string second)
    {
        Assert.Equal(new Drone(first, null).NormalizedName, new Drone(second, null).NormalizedName);
    }
}

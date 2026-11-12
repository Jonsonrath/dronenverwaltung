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

    // Change edit-drones: "Edit drone (REQ-013)".

    [Fact]
    public void REQ_013_EditChangesNameDescriptionAndNormalizedName()
    {
        var drone = new Drone("TELO-07", "rechter Propellerschutz beschädigt");

        drone.Edit("  TELLO-07 ", " Propellerschutz erneuert ");

        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("TELLO-07", drone.NormalizedName);
        Assert.Equal("Propellerschutz erneuert", drone.Description);
    }

    [Fact]
    public void REQ_013_EditWithoutDescriptionStoresEmpty()
    {
        var drone = new Drone("TELLO-07", "alt");

        drone.Edit("TELLO-07", null);

        Assert.Equal(string.Empty, drone.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_013_EditRejectsEmptyNameAndKeepsTheDrone(string name)
    {
        var drone = new Drone("TELLO-07", "Beschreibung");

        Assert.Throws<ValidationException>(() => drone.Edit(name, "neu"));
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("Beschreibung", drone.Description);
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

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

    // Change support-damaged-drones: "Report drone damage (REQ-014)", "Repair drone (REQ-015)".

    [Fact]
    public void REQ_014_ReportDamageStoresTrimmedReason()
    {
        var drone = new Drone("TELLO-07", null);

        drone.ReportDamage(" Propeller gebrochen ");

        Assert.Equal("Propeller gebrochen", drone.DamageReason);
    }

    [Fact]
    public void REQ_014_NewDroneIsNotDamaged()
    {
        Assert.Null(new Drone("TELLO-07", null).DamageReason);
    }

    [Fact]
    public void REQ_014_NewReasonReplacesTheOldOne()
    {
        var drone = new Drone("TELLO-07", null);
        drone.ReportDamage("Propeller gebrochen");

        drone.ReportDamage("Akkufach locker");

        Assert.Equal("Akkufach locker", drone.DamageReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_014_RejectsEmptyReasonAndKeepsTheDrone(string? reason)
    {
        var drone = new Drone("TELLO-07", null);

        var error = Assert.Throws<ValidationException>(() => drone.ReportDamage(reason));

        Assert.Equal("Der Grund für den Defekt darf nicht leer sein.", error.Message);
        Assert.Null(drone.DamageReason);
    }

    [Fact]
    public void REQ_015_RepairRemovesTheReason()
    {
        var drone = new Drone("TELLO-07", null);
        drone.ReportDamage("Propeller gebrochen");

        drone.Repair();

        Assert.Null(drone.DamageReason);
    }

    [Fact]
    public void REQ_015_RepairOfUndamagedDroneIsAConflict()
    {
        var drone = new Drone("TELLO-07", null);

        var error = Assert.Throws<ConflictException>(() => drone.Repair());

        Assert.Equal("Die Drohne \"TELLO-07\" ist nicht als defekt gemeldet.", error.Message);
    }

    // Change drone-ssid: "Drone SSID (REQ-024)".

    [Fact]
    public void REQ_024_CreatesDroneWithTrimmedSsid()
    {
        var drone = new Drone("TELLO-07", null, " TELLO-9FA21B ");

        Assert.Equal("TELLO-9FA21B", drone.Ssid);
        Assert.Equal("TELLO-9FA21B", drone.NormalizedSsid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void REQ_024_EmptySsidMeansNoSsid(string? ssid)
    {
        var drone = new Drone("TELLO-08", null, ssid);

        Assert.Null(drone.Ssid);
        Assert.Null(drone.NormalizedSsid);
    }

    [Fact]
    public void REQ_024_SameNormalizedSsidIgnoringCase()
    {
        Assert.Equal(new Drone("A", null, "TELLO-9FA21B").NormalizedSsid, new Drone("B", null, "tello-9fa21b").NormalizedSsid);
    }

    [Fact]
    public void REQ_024_EditSetsAndRemovesSsid()
    {
        var drone = new Drone("TELLO-07", null);

        drone.Edit("TELLO-07", null, " TELLO-9FA21B ");
        Assert.Equal(("TELLO-9FA21B", "TELLO-9FA21B"), (drone.Ssid, drone.NormalizedSsid));

        drone.Edit("TELLO-07", null, " ");
        Assert.Equal((null, null), (drone.Ssid, drone.NormalizedSsid));
    }
}

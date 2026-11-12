namespace DroneInventory.Api.Domain;

public enum LoanStatus
{
    Active,
    Returned,
}

// A loan of one Drone to one StudentGroup with its accessories (REQ-004, REQ-006).
// Loan is the aggregate root: it owns its LoanItems and controls its status.
// Returned loans are never deleted (REQ-010).
public class Loan
{
    private readonly List<LoanItem> _items = [];

    public int Id { get; private set; }

    public int DroneId { get; private set; }

    public Drone Drone { get; private set; } = null!;

    public int StudentGroupId { get; private set; }

    public StudentGroup StudentGroup { get; private set; } = null!;

    /// <summary>Set by the system when the loan is created (UTC).</summary>
    public DateTime LoanDate { get; private set; }

    /// <summary>Null while the loan is active; set by <see cref="Return"/> (UTC, REQ-009).</summary>
    public DateTime? ReturnDate { get; private set; }

    public LoanStatus Status { get; private set; }

    /// <summary>Always exactly one Battery and one Propeller item, quantities may be 0.</summary>
    public IReadOnlyCollection<LoanItem> Items => _items;

    public int Batteries => QuantityOf(LoanItemType.Battery);

    public int Propellers => QuantityOf(LoanItemType.Propeller);

    /// <summary>Returned batteries (REQ-022); null while active or for loans returned before REQ-022.</summary>
    public int? ReturnedBatteries => Item(LoanItemType.Battery).ReturnedQuantity;

    /// <summary>Returned propellers (REQ-022); null while active or for loans returned before REQ-022.</summary>
    public int? ReturnedPropellers => Item(LoanItemType.Propeller).ReturnedQuantity;

    // For EF Core.
    private Loan()
    {
    }

    /// <summary>
    /// Creates an active loan. Whether the drone is available is checked by the
    /// LoanService and the database (REQ-005); negative quantities throw here (REQ-006).
    /// </summary>
    public Loan(Drone drone, StudentGroup studentGroup, int batteries, int propellers, DateTime loanDate)
    {
        _items.Add(new LoanItem(LoanItemType.Battery, batteries));
        _items.Add(new LoanItem(LoanItemType.Propeller, propellers));

        Drone = drone;
        DroneId = drone.Id;
        StudentGroup = studentGroup;
        StudentGroupId = studentGroup.Id;
        LoanDate = loanDate;
        Status = LoanStatus.Active;
    }

    /// <summary>
    /// Active → Returned and stores the return date (REQ-008, REQ-009) and the returned
    /// batteries and propellers (REQ-022). A number that is not given counts as the loaned
    /// quantity; each number must be 0..loaned quantity. The numbers are validated before
    /// anything changes, so an invalid return leaves the loan active.
    /// Everything else stays unchanged. A loan can only be returned once.
    /// </summary>
    public void Return(DateTime returnDate, int? returnedBatteries = null, int? returnedPropellers = null)
    {
        if (Status == LoanStatus.Returned)
        {
            throw new ConflictException($"Die Ausleihe {Id} wurde bereits zurückgegeben.");
        }

        var battery = Item(LoanItemType.Battery);
        var propeller = Item(LoanItemType.Propeller);
        var batteries = returnedBatteries ?? battery.Quantity;
        var propellers = returnedPropellers ?? propeller.Quantity;
        battery.ValidateReturned(batteries);
        propeller.ValidateReturned(propellers);

        battery.SetReturned(batteries);
        propeller.SetReturned(propellers);
        Status = LoanStatus.Returned;
        ReturnDate = returnDate;
    }

    private int QuantityOf(LoanItemType type) => Item(type).Quantity;

    private LoanItem Item(LoanItemType type) => _items.Single(i => i.Type == type);
}

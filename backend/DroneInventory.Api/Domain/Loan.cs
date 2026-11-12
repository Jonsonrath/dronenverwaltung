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
    /// Active → Returned and stores the return date (REQ-008, REQ-009).
    /// Everything else stays unchanged. A loan can only be returned once.
    /// </summary>
    public void Return(DateTime returnDate)
    {
        if (Status == LoanStatus.Returned)
        {
            throw new ConflictException($"Die Ausleihe {Id} wurde bereits zurückgegeben.");
        }

        Status = LoanStatus.Returned;
        ReturnDate = returnDate;
    }

    private int QuantityOf(LoanItemType type) => _items.Single(i => i.Type == type).Quantity;
}

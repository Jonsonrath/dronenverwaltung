namespace DroneInventory.Api.Domain;

public enum LoanItemType
{
    Battery,
    Propeller,
}

// An accessory position of a Loan: type and quantity (REQ-006).
// Accessories are quantities only, never individual items. A LoanItem exists
// only inside its Loan and is created by it.
public class LoanItem
{
    public int Id { get; private set; }

    public int LoanId { get; private set; }

    public LoanItemType Type { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>
    /// Null while the loan is active (and for loans returned before REQ-022);
    /// set by <see cref="Loan.Return"/>. Missing = Quantity - ReturnedQuantity, derived.
    /// </summary>
    public int? ReturnedQuantity { get; private set; }

    internal LoanItem(LoanItemType type, int quantity)
    {
        if (quantity < 0)
        {
            var what = type == LoanItemType.Battery ? "Akkus" : "Propeller";
            throw new ValidationException($"Die Anzahl der {what} darf nicht negativ sein.");
        }

        Type = type;
        Quantity = quantity;
    }

    /// <summary>Accepts 0..Quantity (REQ-022); everything else is invalid input.</summary>
    internal void ValidateReturned(int returned)
    {
        if (returned < 0 || returned > Quantity)
        {
            var what = Type == LoanItemType.Battery ? "Akkus" : "Propeller";
            throw new ValidationException($"Die Anzahl der zurückgegebenen {what} muss zwischen 0 und {Quantity} liegen.");
        }
    }

    internal void SetReturned(int returned) => ReturnedQuantity = returned;
}

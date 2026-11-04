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

    internal LoanItem(LoanItemType type, int quantity)
    {
        if (quantity < 0)
        {
            throw new ValidationException($"The number of {type.ToString().ToLowerInvariant()}s must not be negative.");
        }

        Type = type;
        Quantity = quantity;
    }
}

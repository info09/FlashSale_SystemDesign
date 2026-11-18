namespace FlashSale.Domain.Models;

public enum HoldCreationStatus
{
    Success = 0,
    InsufficientInventory = 1,
    ActiveHoldAlreadyExists = 2
}

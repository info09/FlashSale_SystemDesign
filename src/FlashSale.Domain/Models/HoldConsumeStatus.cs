namespace FlashSale.Domain.Models;

public enum HoldConsumeStatus
{
    Success = 0,
    NotFound = 1,
    Expired = 2,
    Consumed = 3,
    NotOwned = 4,
    Invalid = 5
}

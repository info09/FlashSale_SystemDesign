namespace FlashSale.Domain.Models;

public enum HoldReleaseStatus
{
    Released = 0,
    NotFound = 1,
    AlreadyReleased = 2,
    Consumed = 3,
    ExpiredReleased = 4,
    Invalid = 5,
    NotOwned = 6
}

namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Hold đã hết hạn (HoldExpiry).
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold, HoldExpiry.
/// </summary>
public class HoldExpiredException : Exception
{
    public Guid HoldId { get; }

    public HoldExpiredException(Guid holdId)
        : base($"Hold '{holdId}' đã hết hạn.")
    {
        HoldId = holdId;
    }
}

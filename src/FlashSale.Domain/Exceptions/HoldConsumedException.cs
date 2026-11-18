namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Hold đã được sử dụng/consume.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold.
/// </summary>
public class HoldConsumedException : Exception
{
    public Guid HoldId { get; }

    public HoldConsumedException(Guid holdId)
        : base($"Hold '{holdId}' đã được sử dụng.")
    {
        HoldId = holdId;
    }
}

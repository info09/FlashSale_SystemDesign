namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Hold bị xác nhận trùng (double confirm).
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold.
/// </summary>
public class HoldAlreadyConfirmedException : Exception
{
    public Guid HoldId { get; }

    public HoldAlreadyConfirmedException(Guid holdId)
        : base($"Hold '{holdId}' đã được xác nhận trước đó.")
    {
        HoldId = holdId;
    }
}

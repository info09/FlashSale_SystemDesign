namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Hold không hợp lệ (không tồn tại hoặc trạng thái không hợp lệ).
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold.
/// </summary>
public class HoldInvalidException : Exception
{
    public Guid HoldId { get; }

    public HoldInvalidException(Guid holdId)
        : base($"Hold '{holdId}' không hợp lệ.")
    {
        HoldId = holdId;
    }
}

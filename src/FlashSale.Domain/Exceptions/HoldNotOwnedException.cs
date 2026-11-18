namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Hold không thuộc về Client hiện tại.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold, Client.
/// </summary>
public class HoldNotOwnedException : Exception
{
    public Guid HoldId { get; }
    public string ClientId { get; }

    public HoldNotOwnedException(Guid holdId, string clientId)
        : base($"Hold '{holdId}' không thuộc về client '{clientId}'.")
    {
        HoldId = holdId;
        ClientId = clientId;
    }
}

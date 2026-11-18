namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi Client đã có Hold đang hoạt động (1 user/1 hold).
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold, Client.
/// </summary>
public class ActiveHoldAlreadyExistsException : Exception
{
    public string ClientId { get; }
    public Guid ExistingHoldId { get; }

    public ActiveHoldAlreadyExistsException(string clientId, Guid existingHoldId)
        : base($"Client '{clientId}' đã có hold đang hoạt động '{existingHoldId}'.")
    {
        ClientId = clientId;
        ExistingHoldId = existingHoldId;
    }
}

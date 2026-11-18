namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity Hold (Giữ chỗ) - trạng thái giữ chỗ tạm thời trong luồng FlashSale.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold, HoldExpiry, Inventory.
/// </summary>
public class Hold
{
    public Guid Id { get; private set; }
    public string ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string ClientId { get; private set; }
    public HoldStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    private Hold() { }

    public Hold(Guid id, string productId, int quantity, string clientId, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId không được để trống", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0", nameof(quantity));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId không được để trống", nameof(clientId));

        Id = id;
        ProductId = productId;
        Quantity = quantity;
        ClientId = clientId;
        Status = HoldStatus.Active;
        ExpiresAt = expiresAt;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void MarkConsumed() => Status = HoldStatus.Consumed;

    public void MarkReleased() => Status = HoldStatus.Released;

    public void MarkExpired() => Status = HoldStatus.Expired;
}

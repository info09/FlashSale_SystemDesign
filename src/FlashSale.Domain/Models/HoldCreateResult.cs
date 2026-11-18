namespace FlashSale.Domain.Models;

/// <summary>
/// Kết quả tạo Hold từ kho lưu trữ (Redis atomic).
/// </summary>
public sealed record HoldCreateResult(
    HoldCreationStatus Status,
    Guid? HoldId = null,
    DateTimeOffset? ExpiresAt = null,
    int TtlSeconds = 0,
    Guid? ExistingHoldId = null,
    int? AvailableQuantity = null);

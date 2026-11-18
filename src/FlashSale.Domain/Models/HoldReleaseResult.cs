namespace FlashSale.Domain.Models;

/// <summary>
/// Kết quả release Hold (idempotent).
/// </summary>
public sealed record HoldReleaseResult(HoldReleaseStatus Status);

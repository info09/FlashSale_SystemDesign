using FlashSale.Domain.Entities;
using FlashSale.Domain.Models;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao diện thao tác Hold (giữ chỗ) - lưu trữ Redis/atomic.
/// Thuật ngữ chuẩn: Hold, HoldExpiry, Inventory, WorkerUnHold.
/// </summary>
public interface IHoldRepository
{
    /// <summary>
    /// Tạo hold và reserve inventory atomic. TTL 60s, enforce 1 user/1 hold.
    /// </summary>
    Task<HoldCreateResult> CreateHoldAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume hold (confirm) atomic: chỉ thành công khi active, đúng owner, chưa hết hạn.
    /// </summary>
    Task<HoldConsumeResult> ConsumeHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release hold và trả inventory (idempotent).
    /// </summary>
    Task<HoldReleaseResult> ReleaseHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy hold theo Id (nếu hỗ trợ bởi implementation).
    /// </summary>
    Task<Hold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken = default);
}

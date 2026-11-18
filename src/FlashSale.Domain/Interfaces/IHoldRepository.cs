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
    /// Tạo hold và reserve inventory atomic (TTL 60s), đồng thời enforce ràng buộc 1 user/1 hold active.
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần giữ chỗ.</param>
    /// <param name="quantity">Số lượng cần giữ chỗ.</param>
    /// <param name="clientId">Định danh client/user.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả tạo hold với trạng thái và thông tin hold khi thành công.</returns>
    Task<HoldCreateResult> CreateHoldAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume hold (confirm) atomic: chỉ thành công khi hold active, đúng owner và chưa hết hạn.
    /// </summary>
    /// <param name="holdId">Id của hold.</param>
    /// <param name="clientId">Định danh client/user xác nhận hold.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả consume và dữ liệu hold khi thành công.</returns>
    Task<HoldConsumeResult> ConsumeHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release hold và trả inventory (idempotent) khi hold còn có thể nhả slot.
    /// </summary>
    /// <param name="holdId">Id của hold.</param>
    /// <param name="clientId">Định danh client/user yêu cầu release.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Trạng thái release/idempotent tương ứng.</returns>
    Task<HoldReleaseResult> ReleaseHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy hold theo Id (nếu implementation hỗ trợ đọc trực tiếp).
    /// </summary>
    /// <param name="holdId">Id của hold.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Hold nếu tồn tại; ngược lại null.</returns>
    Task<Hold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken = default);
}

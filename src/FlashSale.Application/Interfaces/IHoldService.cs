using FlashSale.Application.Features.Hold.Commands;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Dịch vụ Hold ở lớp Application.
/// Thuật ngữ chuẩn: Hold, HoldExpiry, Inventory, InformationCustomer.
/// </summary>
public interface IHoldService
{
    /// <summary>
    /// Tạo hold/giữ chỗ cho sản phẩm theo command (kiểm tra input, reserve inventory, enforce 1 user/1 hold).
    /// </summary>
    /// <param name="command">Thông tin tạo hold (ProductId, Quantity, ClientId).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin hold đã tạo (HoldId, ExpiresAt, TtlSeconds).</returns>
    Task<CreateHoldResult> CreateHoldAsync(CreateHoldCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác nhận hold và submit thông tin khách hàng: consume hold, lưu InformationCustomer, tạo payload Outbox.
    /// </summary>
    /// <param name="command">Thông tin confirm (HoldId, ClientId, Name, PhoneNumber, Address).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả confirm gồm hold/order/correlation và thông tin liên quan.</returns>
    Task<ConfirmHoldResult> ConfirmHoldAsync(ConfirmHoldCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy/release hold theo yêu cầu của client (idempotent theo kết quả repository).
    /// </summary>
    /// <param name="command">Thông tin release (HoldId, ClientId).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả release.</returns>
    Task<ReleaseHoldResult> ReleaseHoldAsync(ReleaseHoldCommand command, CancellationToken cancellationToken = default);
}

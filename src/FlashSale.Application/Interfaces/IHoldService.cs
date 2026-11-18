using FlashSale.Application.Features.Hold.Commands;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Dịch vụ Hold ở lớp Application.
/// Thuật ngữ chuẩn: Hold, HoldExpiry, Inventory, InformationCustomer.
/// </summary>
public interface IHoldService
{
    Task<CreateHoldResult> CreateHoldAsync(CreateHoldCommand command, CancellationToken cancellationToken = default);
    Task<ConfirmHoldResult> ConfirmHoldAsync(ConfirmHoldCommand command, CancellationToken cancellationToken = default);
    Task<ReleaseHoldResult> ReleaseHoldAsync(ReleaseHoldCommand command, CancellationToken cancellationToken = default);
}

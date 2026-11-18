using MediatR;

namespace FlashSale.Application.Features.Hold.Commands;

/// <summary>
/// Command hủy/release Hold.
/// Thuật ngữ chuẩn: Hold, Inventory.
/// </summary>
public record ReleaseHoldCommand(Guid HoldId, string ClientId) : IRequest<ReleaseHoldResult>;

public record ReleaseHoldResult(bool Released);

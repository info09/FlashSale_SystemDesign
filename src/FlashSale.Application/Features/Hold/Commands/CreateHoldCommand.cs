using MediatR;

namespace FlashSale.Application.Features.Hold.Commands;

/// <summary>
/// Command tạo Hold (giữ chỗ) cho sản phẩm.
/// Thuật ngữ chuẩn: Hold, Inventory.
/// </summary>
public record CreateHoldCommand(string ProductId, int Quantity, string ClientId) : IRequest<CreateHoldResult>;

public record CreateHoldResult(Guid HoldId, DateTimeOffset ExpiresAt, int TtlSeconds);

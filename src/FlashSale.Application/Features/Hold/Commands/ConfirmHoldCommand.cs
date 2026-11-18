using MediatR;

namespace FlashSale.Application.Features.Hold.Commands;

/// <summary>
/// Command xác nhận Hold và submit thông tin khách hàng.
/// Thuật ngữ chuẩn: Hold, InformationCustomer, Order.
/// </summary>
public record ConfirmHoldCommand(
    Guid HoldId,
    string ClientId,
    string Name,
    string PhoneNumber,
    string Address) : IRequest<ConfirmHoldResult>;

public record ConfirmHoldResult(
    Guid HoldId,
    Guid OrderId,
    Guid CorrelationId,
    string ProductId,
    int Quantity,
    string ClientId);

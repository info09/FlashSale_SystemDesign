using MediatR;

namespace FlashSale.Application.Features.Order.Commands;

/// <summary>
/// Command yêu cầu tạo đơn hàng (Order) sau khi trừ kho thành công.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, Product, ClientId.
/// </summary>
public record CreateOrderCommand(string ProductId, int Quantity, string ClientId) : IRequest<CreateOrderResult>;

/// <summary>
/// Kết quả trả về sau khi tạo đơn hàng.
/// </summary>
public record CreateOrderResult(Guid OrderId, string OrderStatus, DateTimeOffset CreatedOn);

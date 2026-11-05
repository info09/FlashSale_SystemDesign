using System.Text.Json;
using OrderEntity = FlashSale.Domain.Entities.Order;

namespace FlashSale.Application.Features.Inventory.Events;

/// <summary>
/// Message contract cho sự kiện Order được tạo thành công.
/// Mỗi message phải có: CorrelationId, MessageId, OccurredOn, Payload.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, OrderCreatedMessage.
/// </summary>
public record OrderCreatedMessage
{
    public Guid CorrelationId { get; init; }
    public Guid MessageId { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public object Payload { get; init; }

    public static OrderCreatedMessage Create(OrderEntity order, Guid correlationId)
    {
        return new OrderCreatedMessage
        {
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid(),
            OccurredOn = order.CreatedOn,
            Payload = new
            {
                orderId = order.Id,
                productId = order.ProductId,
                quantity = order.Quantity,
                clientId = order.ClientId,
                orderStatus = order.OrderStatus,
                createdOn = order.CreatedOn
            }
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}

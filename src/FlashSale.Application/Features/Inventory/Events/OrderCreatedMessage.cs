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

    public static OrderCreatedMessage CreateFromHoldConfirmation(
        Guid holdId,
        Guid orderId,
        string productId,
        int quantity,
        string clientId,
        string name,
        string phoneNumber,
        string address,
        Guid correlationId,
        DateTimeOffset occurredOn)
    {
        return new OrderCreatedMessage
        {
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid(),
            OccurredOn = occurredOn,
            Payload = new
            {
                holdId,
                orderId,
                correlationId,
                productId,
                quantity,
                clientId,
                name,
                phoneNumber,
                address,
                orderStatus = "Created",
                occurredOn
            }
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}

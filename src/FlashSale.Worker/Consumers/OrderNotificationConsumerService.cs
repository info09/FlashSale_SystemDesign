using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlashSale.Worker.Consumers;

/// <summary>
/// Consumer downstream tieu thu su kien Outbox da xuat phat tu Queue order_notification_queue.
/// Chi nhan message, xu ly nghiep vụ downstream (vi du: gui email, cap nhat catalog), khong chua business logic phuc tap.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: OrderNotificationConsumerService, Order, Outbox, Queue, Message, AsyncWorker.
/// </summary>
public class OrderNotificationConsumerService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderNotificationConsumerService> _logger;

    public OrderNotificationConsumerService(
        IOrderRepository orderRepository,
        ILogger<OrderNotificationConsumerService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Xu ly mot message OrderNotification tu Queue order_notification_queue.
    /// </summary>
    public async Task ProcessMessageAsync(string json, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var correlationId = Guid.Parse(root.GetProperty("CorrelationId").GetString()!);
        var messageId = Guid.Parse(root.GetProperty("MessageId").GetString()!);
        var occurredOn = DateTimeOffset.Parse(root.GetProperty("OccurredOn").GetString()!);
        var payload = root.GetProperty("Payload");

        var orderId = Guid.Parse(payload.GetProperty("orderId").GetString()!);
        var productId = payload.GetProperty("productId").GetString()!;
        var quantity = payload.GetProperty("quantity").GetInt32();
        var clientId = payload.GetProperty("clientId").GetString()!;
        var orderStatus = payload.GetProperty("orderStatus").GetString() ?? "Created";

        // Kiem tra Order da ton tai trong PostgreSQLPrimary
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} khong ton tai, bo qua message notification {MessageId}.", orderId, messageId);
            return;
        }

        _logger.LogInformation(
            "Da xu ly thong bao Order {OrderId} (ProductId={ProductId}, Quantity={Quantity}, ClientId={ClientId}, Status={OrderStatus}) tu message {MessageId}.",
            orderId, productId, quantity, clientId, orderStatus, messageId);
    }
}

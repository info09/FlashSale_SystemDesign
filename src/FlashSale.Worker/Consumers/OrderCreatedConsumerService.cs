using System.Text.Json;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace FlashSale.Worker.Consumers;

/// <summary>
/// Dich vu tieu thu message OrderCreated tu RabbitMQ Queue (order_pipeline_queue).
/// Ghi Order va su kien Outbox vao PostgreSQLPrimary trong cung 1 transaction (Transactional Outbox Pattern).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: AsyncWorker, Order, Outbox, Queue, Message, PostgreSQLPrimary, TransactionalOutboxPattern.
/// </summary>
public class OrderCreatedConsumerService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderCreatedConsumerService> _logger;

    public OrderCreatedConsumerService(
        IOrderRepository orderRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        ILogger<OrderCreatedConsumerService> logger)
    {
        _orderRepository = orderRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Xu ly mot message OrderCreated tu Queue.
    /// Neu Order da ton tai (theo orderId) thi bo qua (idempotent).
    /// Nguoc lai: tao Order + ghi Outbox trong cung 1 transaction PostgreSQLPrimary.
    /// </summary>
    public async Task ProcessMessageAsync(string json, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var payload = root.GetProperty("Payload");
        var orderId = Guid.Parse(payload.GetProperty("orderId").GetString()!);
        var productId = payload.GetProperty("productId").GetString()!;
        var quantity = payload.GetProperty("quantity").GetInt32();
        var clientId = payload.GetProperty("clientId").GetString()!;
        var orderStatus = payload.GetProperty("orderStatus").GetString() ?? "Created";
        var correlationId = Guid.Parse(root.GetProperty("CorrelationId").GetString()!);
        var messageId = Guid.Parse(root.GetProperty("MessageId").GetString()!);

        // Idempotency: neu Order da ton tai thi bo qua (khong tao trung lap)
        var existing = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Order {OrderId} da ton tai, bo qua message {MessageId}.", orderId, messageId);
            return;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Tao Order
            var order = new Order(orderId, productId, quantity, clientId);
            order.UpdateStatus(orderStatus);
            await _orderRepository.AddAsync(order, cancellationToken);

            // Ghi su kien vao Outbox (cung transaction voi Order)
            var outboxPayload = JsonSerializer.Serialize(new
            {
                orderId = order.Id,
                status = orderStatus,
                processedOn = DateTimeOffset.UtcNow
            });
            await _outboxService.AddOutboxAsync(order.Id, "OrderProcessed", outboxPayload, correlationId, cancellationToken);

            // Commit transaction: luu Order + Outbox trong cung 1 transaction PostgreSQLPrimary
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Da xu ly Order {OrderId} tu message {MessageId} (Queue {Queue}).",
                orderId, messageId, RabbitMqConstants.QueueName);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

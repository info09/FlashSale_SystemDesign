using System.Text.Json;
using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ xuất phát (publish) các sự kiện đã lưu trong bảng Outbox ra RabbitMQ.
/// Dùng trong Transactional Outbox Pattern (FR-04, NFR-06).
/// </summary>
public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;
    private readonly IUnitOfWork _unitOfWork;

    public OutboxPublisher(
        IOutboxRepository outboxRepository,
        IRabbitMqPublisherService rabbitMqPublisherService,
        IUnitOfWork unitOfWork)
    {
        _outboxRepository = outboxRepository;
        _rabbitMqPublisherService = rabbitMqPublisherService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Xuất phát các ghi Outbox chưa được xử lý ra RabbitMQ theo loại sự kiện.
    /// </summary>
    public async Task PublishPendingOutboxAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 100;
        var unprocessedOutboxes = await _outboxRepository.GetUnprocessedAsync(batchSize, cancellationToken);

        foreach (var outbox in unprocessedOutboxes)
        {
            try
            {
                await PublishOutboxAsync(outbox, cancellationToken);
                outbox.MarkAsProcessed();
                await _outboxRepository.UpdateAsync(outbox, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                outbox.MarkAsFailed();
                await _outboxRepository.UpdateAsync(outbox, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task PublishOutboxAsync(Outbox outbox, CancellationToken cancellationToken)
    {
        if (string.Equals(outbox.EventType, "HoldConfirmed", StringComparison.OrdinalIgnoreCase))
        {
            await PublishHoldConfirmedToPipelineAsync(outbox, cancellationToken);
            return;
        }

        var notificationMessageJson = JsonSerializer.Serialize(new
        {
            CorrelationId = outbox.CorrelationId,
            MessageId = outbox.Id,
            OccurredOn = outbox.OccurredOn,
            EventType = outbox.EventType,
            Payload = JsonSerializer.Deserialize<object>(outbox.Payload)
        });

        await _rabbitMqPublisherService.PublishAsync(
            RabbitMqConstants.OrderNotificationExchangeName,
            RabbitMqConstants.OrderNotificationRoutingKey,
            notificationMessageJson,
            outbox.CorrelationId,
            outbox.Id,
            cancellationToken);
    }

    private async Task PublishHoldConfirmedToPipelineAsync(Outbox outbox, CancellationToken cancellationToken)
    {
        using var payloadDoc = JsonDocument.Parse(outbox.Payload);
        var payloadRoot = payloadDoc.RootElement;

        var orderId = payloadRoot.GetProperty("orderId").GetGuid();
        var holdId = payloadRoot.TryGetProperty("holdId", out var holdIdElement) && holdIdElement.ValueKind != JsonValueKind.Null
            ? holdIdElement.GetGuid()
            : (Guid?)null;
        var productId = payloadRoot.GetProperty("productId").GetString() ?? string.Empty;
        var quantity = payloadRoot.GetProperty("quantity").GetInt32();
        var clientId = payloadRoot.GetProperty("clientId").GetString() ?? string.Empty;
        var orderStatus = payloadRoot.TryGetProperty("orderStatus", out var statusElement)
            ? statusElement.GetString() ?? "Created"
            : "Created";
        var name = payloadRoot.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
        var phoneNumber = payloadRoot.TryGetProperty("phoneNumber", out var phoneElement) ? phoneElement.GetString() : null;
        var address = payloadRoot.TryGetProperty("address", out var addressElement) ? addressElement.GetString() : null;
        var occurredOn = payloadRoot.TryGetProperty("occurredOn", out var occurredElement) && occurredElement.ValueKind == JsonValueKind.String
            ? DateTimeOffset.Parse(occurredElement.GetString()!)
            : outbox.OccurredOn;

        object payload = holdId.HasValue && name is not null && phoneNumber is not null && address is not null
            ? new
            {
                holdId = holdId.Value,
                orderId,
                correlationId = outbox.CorrelationId,
                productId,
                quantity,
                clientId,
                name,
                phoneNumber,
                address,
                orderStatus,
                occurredOn
            }
            : new
            {
                orderId,
                productId,
                quantity,
                clientId,
                orderStatus,
                createdOn = occurredOn
            };

        var message = new OrderCreatedMessage
        {
            CorrelationId = outbox.CorrelationId,
            MessageId = outbox.Id,
            OccurredOn = occurredOn,
            Payload = payload
        };

        await _rabbitMqPublisherService.PublishOrderCreatedAsync(message, cancellationToken);
    }
}

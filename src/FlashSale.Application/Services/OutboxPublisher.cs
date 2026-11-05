using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using System.Text.Json;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ xuất phat (publish) các sự kiện đã lưu trong bảng Outbox ra Message Broker (RabbitMQ).
/// Dùng trong Transactional Outbox Pattern (FR-04, NFR-06): chốt dữ liệu vào Outbox, sau đó xuất phat ra Queue.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Outbox, IOutboxPublisher, RabbitMQ, Queue, OrderPipeline, Message.
/// </summary>
public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;

    public OutboxPublisher(IOutboxRepository outboxRepository, IRabbitMqPublisherService rabbitMqPublisherService)
    {
        _outboxRepository = outboxRepository;
        _rabbitMqPublisherService = rabbitMqPublisherService;
    }

    /// <summary>
    /// Xuất phat tất cả các ghi Outbox chưa được xử lý ra RabbitMQ Queue (OrderPipeline).
    /// Sau khi xuất phat thành công, đánh dấu ghi Outbox đã được xử lý.
    /// </summary>
    public async Task PublishPendingOutboxAsync(CancellationToken cancellationToken = default)
    {
        var pendingOutboxes = await _outboxRepository.GetUnprocessedAsync(100, cancellationToken);

        foreach (var outbox in pendingOutboxes)
        {
            try
            {
                // Xây dựng OrderCreatedMessage từ nội dung Outbox
                var payloadObj = JsonSerializer.Deserialize<object>(outbox.Payload)
                    ?? throw new InvalidOperationException($"Không thể deserialize Payload của Outbox Id {outbox.Id}");

                var message = new OrderCreatedMessage
                {
                    CorrelationId = outbox.CorrelationId,
                    MessageId = outbox.Id,
                    OccurredOn = outbox.OccurredOn,
                    Payload = payloadObj
                };

                await _rabbitMqPublisherService.PublishOrderCreatedAsync(message, cancellationToken);

                outbox.MarkAsProcessed();
                await _outboxRepository.UpdateAsync(outbox, cancellationToken);
            }
            catch
            {
                outbox.MarkAsFailed();
                await _outboxRepository.UpdateAsync(outbox, cancellationToken);
            }
        }
    }
}

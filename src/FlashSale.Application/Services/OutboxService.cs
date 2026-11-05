using System.Text.Json;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ Outbox ở lớp Application, quản lý ghi sự kiện vào bảng Outbox.
/// Dùng trong Transactional Outbox Pattern (FR-04, NFR-06): ghi Order và Outbox trong cùng 1 transaction,
/// đảm bảo không mất sự kiện đã nhận 202 Accepted.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Outbox, IOutboxService, PostgreSQLPrimary, TransactionalOutboxPattern.
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _outboxRepository;

    public OutboxService(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    /// <summary>
    /// Thêm một ghi Outbox mới (chưa được xử lý) liên quan đến một Order.
    /// </summary>
    public async Task AddOutboxAsync(Guid orderId, string eventType, string payload, Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId không được để trống", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType không được để trống", nameof(eventType));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload không được để trống", nameof(payload));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId không được để trống", nameof(correlationId));

        // Validate payload là JSON hợp lệ
        _ = JsonSerializer.Deserialize<object>(payload)
            ?? throw new ArgumentException("Payload phải là chuỗi JSON hợp lệ", nameof(payload));

        var outbox = new Outbox(orderId, eventType, payload, correlationId);
        await _outboxRepository.AddAsync(outbox, cancellationToken);
    }

    /// <summary>
    /// Lấy danh sách các ghi Outbox chưa được xử lý.
    /// </summary>
    public Task<List<Outbox>> GetUnprocessedOutboxAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit phải lớn hơn 0", nameof(limit));

        return _outboxRepository.GetUnprocessedAsync(limit, cancellationToken);
    }

    /// <summary>
    /// Đánh dấu một ghi Outbox đã xử lý thành công.
    /// </summary>
    public async Task MarkAsProcessedAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId không được để trống", nameof(outboxId));

        var outbox = await _outboxRepository.GetByIdAsync(outboxId, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy Outbox Id {outboxId}.");

        outbox.MarkAsProcessed();
        await _outboxRepository.UpdateAsync(outbox, cancellationToken);
    }

    /// <summary>
    /// Đánh dấu một ghi Outbox xử lý thất bại, tăng số lần thử lại.
    /// </summary>
    public async Task MarkAsFailedAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId không được để trống", nameof(outboxId));

        var outbox = await _outboxRepository.GetByIdAsync(outboxId, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy Outbox Id {outboxId}.");

        outbox.MarkAsFailed();
        await _outboxRepository.UpdateAsync(outbox, cancellationToken);
    }
}

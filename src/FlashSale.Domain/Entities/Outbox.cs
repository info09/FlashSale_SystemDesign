namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity Outbox luu su kien lien quan den don hang trong PostgreSQL Primary.
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06): ghi Order va Outbox trong cung 1 transaction,
/// dam bao khong mat su kien da nhan 202 Accepted.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, PostgreSQLPrimary, Order, DataConsistency, Reconciliation.
/// Khong duoc goi la: OutboxTable, EventStore, AuditLog, MessageOutbox, BangNgoai.
/// </summary>
public class Outbox
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string ProcessingStatus { get; private set; } // Pending, Published, Failed, MovedToDeadLetterQueue
    public int RetryCount { get; private set; }

    private Outbox() { }

    /// <summary>
    /// Khoi tao mot ghi Outbox moi chua duoc xu ly.
    /// </summary>
    public Outbox(Guid orderId, string eventType, string payload, Guid correlationId)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId khong duoc trong", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType khong duoc de trong", nameof(eventType));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload khong duoc de trong", nameof(payload));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId khong duoc trong", nameof(correlationId));

        Id = Guid.NewGuid();
        OrderId = orderId;
        EventType = eventType;
        Payload = payload;
        CorrelationId = correlationId;
        OccurredOn = DateTimeOffset.UtcNow;
        ProcessedOn = null;
        ProcessingStatus = "Pending";
        RetryCount = 0;
    }

    /// <summary>
    /// Khoi tao mot ghi Outbox co Id chinh xac (dung khi load tu kho du lieu/PostgreSQLPrimary).
    /// </summary>
    public Outbox(Guid id, Guid orderId, string eventType, string payload, DateTimeOffset occurredOn, Guid correlationId, string processingStatus, int retryCount, DateTimeOffset? processedOn)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id khong duoc trong", nameof(id));
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId khong duoc trong", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType khong duoc de trong", nameof(eventType));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload khong duoc de trong", nameof(payload));
        if (string.IsNullOrWhiteSpace(processingStatus))
            throw new ArgumentException("ProcessingStatus khong duoc de trong", nameof(processingStatus));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId khong duoc trong", nameof(correlationId));
        if (retryCount < 0)
            throw new ArgumentException("RetryCount phai lon hon hoac bang 0", nameof(retryCount));

        Id = id;
        OrderId = orderId;
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        CorrelationId = correlationId;
        ProcessingStatus = processingStatus;
        RetryCount = retryCount;
        ProcessedOn = processedOn;
    }

    /// <summary>
    /// Danh dau da xu ly thanh cong (Public).
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessingStatus = "Published";
        ProcessedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Danh dau that bai xu ly, tang so lan thu lai.
    /// </summary>
    public void MarkAsFailed()
    {
        ProcessingStatus = "Failed";
        RetryCount++;
        ProcessedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Tang so lan thu lai khi chua xu ly xong.
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
    }
}

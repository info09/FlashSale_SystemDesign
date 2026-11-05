using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich vu Outbox o lop Application, quan ly ghi su kien vao bang Outbox.
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, IOutboxService, PostgreSQLPrimary, TransactionalOutboxPattern.
/// Khong duoc goi la: IOutboxManager, IOutboxStore, IEventStoreService.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Them mot ghi Outbox moi (chua duoc xu ly) lien quan den mot Order.
    /// </summary>
    /// <param name="orderId">Id cua Order lien quan.</param>
    /// <param name="eventType">Loai su kien (vi du: OrderCreated).</param>
    /// <param name="payload">Noi dung su kien dang chuyen doi JSON.</param>
    /// <param name="correlationId">Id lien ket truy xuat qua he thong.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task AddOutboxAsync(Guid orderId, string eventType, string payload, Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach cac ghi Outbox chua duoc xu ly (ProcessingStatus = Pending hoac Failed va RetryCount < MaxRetry).
    /// </summary>
    /// <param name="limit">So luong ghi toi da can lay.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<List<Outbox>> GetUnprocessedOutboxAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh dau mot ghi Outbox da xu ly thanh cong (ProcessingStatus = Published).
    /// </summary>
    /// <param name="outboxId">Id cua ghi Outbox can cap nhat.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task MarkAsProcessedAsync(Guid outboxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh dau mot ghi Outbox that bai xu ly (ProcessingStatus = Failed), tang so lan thu lai.
    /// </summary>
    /// <param name="outboxId">Id cua ghi Outbox can cap nhat.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task MarkAsFailedAsync(Guid outboxId, CancellationToken cancellationToken = default);
}

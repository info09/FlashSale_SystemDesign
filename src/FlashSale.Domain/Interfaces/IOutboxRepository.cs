using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dinh nghia thao tac truy cap Outbox trong kho du lieu (PostgreSQLPrimary).
/// O lop Domain chi dinh nghia contract, chua viet logic thuc thi.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, IOutboxRepository, PostgreSQLPrimary, TransactionalOutboxPattern.
/// Khong duoc goi la: IOutboxDao, IOutboxService, IEventStoreRepository, IAuditLogRepository.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Lay Outbox theo Id.
    /// </summary>
    Task<Outbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Them ghi Outbox moi vao kho du lieu.
    /// </summary>
    Task AddAsync(Outbox outbox, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh sach cac ghi Outbox chua duoc xu ly (ProcessingStatus = Pending hoac Failed va RetryCount < MaxRetry).
    /// </summary>
    Task<List<Outbox>> GetUnprocessedAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat trang thai xu ly cua mot ghi Outbox.
    /// </summary>
    Task UpdateAsync(Outbox outbox, CancellationToken cancellationToken = default);
}

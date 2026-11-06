using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich vu purge/archive bang Outbox de tranh bang lon dan theo thoi gian (NFR-06).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, Purge, PostgreSQLPrimary.
/// Khong duoc goi la: IOutboxPurger, IOutboxArchiver, IOutboxCleaner.
/// </summary>
public interface IOutboxPurgeService
{
    /// <summary>
    /// Xoa (purge) cac ghi Outbox da duoc xu ly (Published/Failed) va cu hon PurgeDays ngay.
    /// Tra ve so luong ghi da duoc xoa.
    /// </summary>
    Task<int> PurgeProcessedOutboxesAsync(CancellationToken cancellationToken = default);
}

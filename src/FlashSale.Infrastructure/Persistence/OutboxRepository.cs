using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IOutboxRepository dung PostgreSQLPrimary.
/// Map giua Outbox domain entity va bang Outbox trong DB.
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, IOutboxRepository, PostgreSQLPrimary, TransactionalOutboxPattern.
/// Khong duoc goi la: IOutboxDao, IOutboxService, IEventStoreRepository, IAuditLogRepository.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private const int MaxRetryCount = 3; // toi da 3 lan thu lai (FR-05)

    private readonly PostgreSQLPrimaryContext _context;

    public OutboxRepository(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    public async Task<Outbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Outboxes.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(Outbox outbox, CancellationToken cancellationToken = default)
    {
        await _context.Outboxes.AddAsync(outbox, cancellationToken);
    }

    public async Task<List<Outbox>> GetUnprocessedAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit phai lon hon 0", nameof(limit));

        return await _context.Outboxes
            .Where(o => o.ProcessingStatus == "Pending"
                || (o.ProcessingStatus == "Failed" && o.RetryCount < MaxRetryCount))
            .OrderBy(o => o.OccurredOn)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Outbox outbox, CancellationToken cancellationToken = default)
    {
        _context.Outboxes.Update(outbox);
    }
}

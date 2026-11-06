using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FlashSale.Application.Services;

/// <summary>
/// Dich vu purge/archive bang Outbox de tranh bang lon dan theo thoi gian (NFR-06).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, PurgeService, PostgreSQLPrimary.
/// </summary>
public class OutboxPurgeService : IOutboxPurgeService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly OutboxPurgeOptions _options;

    public OutboxPurgeService(IOutboxRepository outboxRepository, IOptions<OutboxPurgeOptions> options)
    {
        _outboxRepository = outboxRepository;
        _options = options.Value;
    }

    /// <summary>
    /// Xoa (purge) cac ghi Outbox da duoc xu ly (Published/Failed) va cu hon PurgeDays ngay.
    /// </summary>
    public async Task<int> PurgeProcessedOutboxesAsync(CancellationToken cancellationToken = default)
    {
        var olderThan = DateTimeOffset.UtcNow.AddDays(-_options.PurgeDays);
        return await _outboxRepository.PurgeProcessedOutboxesOlderThanAsync(olderThan, cancellationToken);
    }
}

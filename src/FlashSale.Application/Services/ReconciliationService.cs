using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dich vu Reconciliation o lop Application, thuc hien doi chieu du lieu
/// xac nhan 100% don 202 Accepted da co ban ghi trong PostgreSQLPrimary (NFR-06).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Reconciliation, ReconciliationService, DataConsistency, Order, Outbox, PostgreSQLPrimary.
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly IReconciliationRepository _reconciliationRepository;

    public ReconciliationService(IReconciliationRepository reconciliationRepository)
    {
        _reconciliationRepository = reconciliationRepository;
    }

    /// <summary>
    /// Thuc hien quy trinh Reconciliation: doi chieu tat ca ghi Outbox (don da nhan 202 Accepted)
    /// voi bang Order trong PostgreSQLPrimary.
    /// </summary>
    public async Task<ReconciliationResult> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        // Lay tat ca OrderId da co trong PostgreSQLPrimary (don da ghi thanh cong)
        var existingOrderIds = await _reconciliationRepository.GetAllOrderIdsAsync(cancellationToken);
        var existingOrderIdSet = new HashSet<Guid>(existingOrderIds);

        // Lay tat ca ghi Outbox (don da nhan 202 Accepted, co the Pending/Published/Failed)
        var allOutboxes = await _reconciliationRepository.GetAllOutboxesAsync(cancellationToken);

        var missingCorrelationIds = new List<Guid>();
        var matchedCount = 0;

        foreach (var outbox in allOutboxes)
        {
            if (existingOrderIdSet.Contains(outbox.OrderId))
            {
                matchedCount++;
            }
            else
            {
                // Don da nhan 202 Accepted (co Outbox) ma chua co Order trong PostgreSQLPrimary -> don bi mat
                missingCorrelationIds.Add(outbox.CorrelationId);
            }
        }

        return new ReconciliationResult
        {
            TotalChecked = allOutboxes.Count,
            MatchedCount = matchedCount,
            MissingOrderCount = missingCorrelationIds.Count,
            MissingCorrelationIds = missingCorrelationIds
        };
    }
}

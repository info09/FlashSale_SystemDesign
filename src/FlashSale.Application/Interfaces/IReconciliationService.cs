using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich vu Reconciliation o lop Application, thuc hien doi chieu du lieu
/// xac nhan 100% don 202 Accepted da co ban ghi trong PostgreSQLPrimary (NFR-06).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Reconciliation, IReconciliationService, DataConsistency.
/// Khong duoc goi la: IReconciliationManager, IDataConsistencyService.
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// Thuc hien quy trinh Reconciliation: doi chieu tat ca ghi Outbox (don da nhan 202 Accepted)
    /// voi bang Order trong PostgreSQLPrimary, tra ve ket qua doi chieu.
    /// </summary>
    Task<ReconciliationResult> ReconcileAsync(CancellationToken cancellationToken = default);
}

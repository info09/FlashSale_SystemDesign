using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dinh nghia thao tac truy cap du lieu can thiet cho Reconciliation (doi chieu du lieu).
/// O lop Domain chi dinh nghia contract, chua viet logic thuc thi.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Reconciliation, IReconciliationRepository, Order, Outbox, PostgreSQLPrimary.
/// Khong duoc goi la: IReconciliationDao, IReconciliationStore, IDataConsistencyRepository.
/// </summary>
public interface IReconciliationRepository
{
    /// <summary>
    /// Lay danh tat ca Id cua Order da co trong PostgreSQLPrimary (da ghi thanh cong).
    /// </summary>
    Task<List<Guid>> GetAllOrderIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay danh tat ca ghi Outbox da ghi (Pending, Published, Failed) - thay dien cho danh sach don da nhan 202 Accepted.
    /// </summary>
    Task<List<Outbox>> GetAllOutboxesAsync(CancellationToken cancellationToken = default);
}

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dien Unit of Work, quan ly transaction co so du lieu (PostgreSQLPrimary).
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06): ghi Order va Outbox trong cung 1 transaction,
/// dam bao khong mat su kien da nhan 202 Accepted.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: PostgreSQLPrimary, TransactionalOutboxPattern, DataConsistency.
/// Khong duoc goi la: IUnitOfWorkDao, ITransactionManager, IDbTransaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Bat dau mot transaction moi tren PostgreSQLPrimary.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit transaction hien tai, luu tat ca thay doi vao PostgreSQLPrimary.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback transaction hien tai, huy tat ca thay doi chua duoc commit.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Luu tat ca thay doi chua duoc commit vao PostgreSQLPrimary (flush cac entity da them/cap nhat).
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IReconciliationRepository dung PostgreSQLPrimary (EF Core).
/// Truy van thuc su tu bang Order va Outbox de thuc hien Reconciliation (NFR-06).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: ReconciliationRepository, PostgreSQLPrimary, Order, Outbox.
/// Khong duoc goi la: IReconciliationDao, EfReconciliationRepository.
/// </summary>
public class ReconciliationRepository : IReconciliationRepository
{
    private readonly PostgreSQLPrimaryContext _context;

    public ReconciliationRepository(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lay danh tat ca Id cua Order da co trong PostgreSQLPrimary.
    /// </summary>
    public async Task<List<Guid>> GetAllOrderIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lay danh tat ca ghi Outbox da ghi (Pending, Published, Failed) - thay dien cho danh sach don da nhan 202 Accepted.
    /// </summary>
    public async Task<List<Outbox>> GetAllOutboxesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Outboxes
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

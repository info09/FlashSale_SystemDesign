using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IUnitOfWork bao quanh PostgreSQLPrimaryContext.Database.
/// Dung trong Transactional Outbox Pattern (FR-04, NFR-06).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: PostgreSQLPrimary, TransactionalOutboxPattern, IUnitOfWork.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly PostgreSQLPrimaryContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Khong co transaction nao dang mo de commit.");

        try
        {
            // Luu tat ca thay doi (Order, Outbox da them/cap nhat) vao transaction truoc khi commit
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}

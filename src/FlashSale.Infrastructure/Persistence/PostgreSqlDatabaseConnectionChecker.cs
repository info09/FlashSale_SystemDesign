using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IDatabaseConnectionChecker dung PostgreSQLPrimary (EF Core).
/// Thuat ngua chuan theo docs/domain-glossary.md: PostgreSqlDatabaseConnectionChecker, PostgreSQLPrimary, HealthCheck.
/// Khong duoc goi la: DatabaseHealthChecker, DbConnectionChecker.
/// </summary>
public class PostgreSqlDatabaseConnectionChecker : IDatabaseConnectionChecker
{
    private readonly PostgreSQLPrimaryContext _context;

    public PostgreSqlDatabaseConnectionChecker(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiem tra co the ket noi den PostgreSQLPrimary khong.
    /// </summary>
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.CanConnectAsync(cancellationToken);
    }
}

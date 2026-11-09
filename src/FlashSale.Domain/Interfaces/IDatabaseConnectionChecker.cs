namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dien kiem tra ket noi co so du lieu cho HealthCheck.
/// O lop Domain chi dinh nghia contract, chua viet logic thuc thi.
/// Thuat ngua chuan theo docs/domain-glossary.md: IDatabaseConnectionChecker, PostgreSQLPrimary, HealthCheck.
/// Khong duoc goi la: IDatabaseHealthChecker, IDbConnectionChecker.
/// </summary>
public interface IDatabaseConnectionChecker
{
    /// <summary>
    /// Kiem tra co the ket noi den co so du lieu khong.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}

using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien dich vu HealthCheck o lop Application, kiem tra tinh trang suc khoe
/// cua cac dich vao: PostgreSQLPrimary, Redis, RabbitMQ (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: IHealthCheckService, HealthCheck.
/// Khong duoc goi la: HealthManager, HealthService, StatusService.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Kiem tra tinh trang suc khoe cua tat ca cac dich vao.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<HealthResult> CheckAllAsync(CancellationToken cancellationToken = default);
}

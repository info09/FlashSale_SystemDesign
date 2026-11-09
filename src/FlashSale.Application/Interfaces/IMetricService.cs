using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dich vu Metric o lop Application, thong ke va xuat cac chi so hieu suat
/// (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) de Prometheus scrape (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: IMetricService, Metric, Prometheus.
/// Khong duoc goi la: IMetricManager, IMetricsService, IStatisticsService.
/// </summary>
public interface IMetricService
{
    /// <summary>
    /// Lay tat ca cac metric hien tai cua dich vu.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<List<Metric>> GetAllMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay mot metric theo ten.
    /// </summary>
    /// <param name="name">Ten chi so (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining).</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<Metric?> GetMetricAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat gia tri mot metric.
    /// </summary>
    /// <param name="name">Ten chi so.</param>
    /// <param name="value">Gia tri chi so.</param>
    /// <param name="unit">Don vi tinh.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task SetMetricAsync(string name, double value, string unit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuat tat ca metric theo dang Prometheus text format de Prometheus scrape.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<string> ExportPrometheusFormatAsync(CancellationToken cancellationToken = default);
}

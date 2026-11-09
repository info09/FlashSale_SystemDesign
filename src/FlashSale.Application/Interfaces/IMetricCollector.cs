using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao dien thu thap metric tu cac dich vao infra (Redis, RabbitMQ, PostgreSQL).
/// O lop Application chi dinh nghia contract, chua viet logic thuc thi.
/// Thuat ngua chuan theo docs/domain-glossary.md: IMetricCollector, Metric, Redis, RabbitMQ, PostgreSQLPrimary.
/// Khong duoc goi la: IMetricsCollector, IMetricGatherer, IStatisticsCollector.
/// </summary>
public interface IMetricCollector
{
    /// <summary>
    /// Lay so luong ton kho con lai tren Redis (InventoryRemaining).
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<double> GetInventoryRemainingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay so luong message dang cho trong RabbitMQ Queue (order_pipeline_queue) (QueueDepth).
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<double> GetQueueDepthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay so luong message trong Dead Letter Queue (order_pipeline_dlq) (DlqSize).
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    Task<double> GetDlqSizeAsync(CancellationToken cancellationToken = default);
}

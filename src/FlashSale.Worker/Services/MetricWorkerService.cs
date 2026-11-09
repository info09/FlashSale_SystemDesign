using FlashSale.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace FlashSale.Worker.Services;

/// <summary>
/// Background Service thu thap metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining)
/// dinh ky va cap nhat vao MetricService de Prometheus scrape (FR-06, NFR-07).
/// Chay doc ly tu Worker chinh (ApiGateway).
/// Thuat ngua chuan theo docs/domain-glossary.md: MetricWorkerService, Metric, Prometheus, AsyncWorker.
/// </summary>
public class MetricWorkerService : BackgroundService
{
    private readonly ILogger<MetricWorkerService> _logger;
    private readonly IMetricService _metricService;
    private readonly TimeSpan _collectInterval;

    public MetricWorkerService(
        ILogger<MetricWorkerService> logger,
        IMetricService metricService)
    {
        _logger = logger;
        _metricService = metricService;
        _collectInterval = TimeSpan.FromSeconds(30); // thu thap metric moi 30 giay mot lan
    }

    /// <summary>
    /// Vong lap chinh cua Background Service: thu thap metric dinh ky cho den khi dung.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricWorkerService is running. Collect interval: {Interval}.", _collectInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi thu thap metric dinh ky.");
            }

            await Task.Delay(_collectInterval, stoppingToken);
        }

        _logger.LogInformation("MetricWorkerService is stopping.");
    }

    /// <summary>
    /// Thu thap metric mot lan: goi MetricService de cap nhat cac metric tu infra (InventoryRemaining, QueueDepth, DlqSize).
    /// </summary>
    private async Task CollectOnceAsync(CancellationToken stoppingToken)
    {
        var metrics = await _metricService.GetAllMetricsAsync(stoppingToken);

        _logger.LogDebug("Da thu thap {Count} metric: {Metrics}.",
            metrics.Count,
            string.Join(", ", metrics.Select(m => $"{m.Name}={m.Value}{m.Unit}")));
    }
}

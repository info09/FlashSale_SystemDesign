using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace FlashSale.Application.Services;

/// <summary>
/// Dich vu Metric o lop Application, thong ke va xuat cac chi so hieu suat
/// (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining) de Prometheus scrape (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: MetricService, Metric, Prometheus.
/// </summary>
public class MetricService : IMetricService
{
    private readonly IMetricCollector _metricCollector;
    private readonly ILogger<MetricService> _logger;
    private readonly Dictionary<string, Metric> _metrics;

    public MetricService(
        IMetricCollector metricCollector,
        ILogger<MetricService> logger)
    {
        _metricCollector = metricCollector;
        _logger = logger;
        _metrics = new Dictionary<string, Metric>(StringComparer.OrdinalIgnoreCase);

        // Khoi tao cac metric mac dinh
        _metrics["P99Latency"] = new Metric { Name = "P99Latency", Value = 0, Unit = "ms" };
        _metrics["ErrorRate"] = new Metric { Name = "ErrorRate", Value = 0, Unit = "percent" };
        _metrics["QueueDepth"] = new Metric { Name = "QueueDepth", Value = 0, Unit = "count" };
        _metrics["DlqSize"] = new Metric { Name = "DlqSize", Value = 0, Unit = "count" };
        _metrics["InventoryRemaining"] = new Metric { Name = "InventoryRemaining", Value = 0, Unit = "count" };
    }

    /// <summary>
    /// Lay tat ca cac metric hien tai cua dich vu.
    /// </summary>
    public async Task<List<Metric>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        // Cap nhat cac metric tu infra (InventoryRemaining, QueueDepth, DlqSize)
        await UpdateInfrastructureMetricsAsync(cancellationToken);

        return _metrics.Values.ToList();
    }

    /// <summary>
    /// Lay mot metric theo ten.
    /// </summary>
    public async Task<Metric?> GetMetricAsync(string name, CancellationToken cancellationToken = default)
    {
        await UpdateInfrastructureMetricsAsync(cancellationToken);

        if (_metrics.TryGetValue(name, out var metric))
            return metric;

        return null;
    }

    /// <summary>
    /// Cap nhat gia tri mot metric.
    /// </summary>
    public Task SetMetricAsync(string name, double value, string unit, CancellationToken cancellationToken = default)
    {
        _metrics[name] = new Metric { Name = name, Value = value, Unit = unit, CollectedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    /// <summary>
    /// Xuat tat ca metric theo dang Prometheus text format de Prometheus scrape.
    /// </summary>
    public Task<string> ExportPrometheusFormatAsync(CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();

        foreach (var metric in _metrics.Values.OrderBy(m => m.Name))
        {
            lines.Add($"# HELP flash_sale_{metric.Name.ToLowerInvariant()} {metric.Name} metric");
            lines.Add($"# TYPE flash_sale_{metric.Name.ToLowerInvariant()} gauge");
            lines.Add($"flash_sale_{metric.Name.ToLowerInvariant()} {metric.Value}");
            lines.Add(string.Empty);
        }

        return Task.FromResult(string.Join(Environment.NewLine, lines));
    }

    private async Task UpdateInfrastructureMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var inventoryRemaining = await _metricCollector.GetInventoryRemainingAsync(cancellationToken);
            _metrics["InventoryRemaining"] = new Metric
            {
                Name = "InventoryRemaining",
                Value = inventoryRemaining,
                Unit = "count",
                CollectedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay metric InventoryRemaining.");
        }

        try
        {
            var queueDepth = await _metricCollector.GetQueueDepthAsync(cancellationToken);
            _metrics["QueueDepth"] = new Metric
            {
                Name = "QueueDepth",
                Value = queueDepth,
                Unit = "count",
                CollectedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay metric QueueDepth.");
        }

        try
        {
            var dlqSize = await _metricCollector.GetDlqSizeAsync(cancellationToken);
            _metrics["DlqSize"] = new Metric
            {
                Name = "DlqSize",
                Value = dlqSize,
                Unit = "count",
                CollectedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the lay metric DlqSize.");
        }
    }
}

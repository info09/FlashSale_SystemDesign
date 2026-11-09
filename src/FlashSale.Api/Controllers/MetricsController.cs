using FlashSale.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quan ly xuat Metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining)
/// de Prometheus scrape (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: Metric, MetricsController, Prometheus, ApiGateway.
/// Khong duoc goi la: MetricsApi, StatsController, MonitoringController.
/// </summary>
[ApiController]
[Route("api/v1/metrics")]
[ApiExplorerSettings(GroupName = "v1")]
public class MetricsController : ControllerBase
{
    private readonly IMetricService _metricService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IMetricService metricService,
        ILogger<MetricsController> logger)
    {
        _metricService = metricService;
        _logger = logger;
    }

    /// <summary>
    /// Xuat tat ca metric theo dang Prometheus text format de Prometheus scrape.
    /// Tra ve text/plain voi cau hinh gauge cho tung chi so.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    /// <returns>Prometheus text format cua tat ca metric.</returns>
    /// <response code="200">Tra ve metric dang Prometheus text format.</response>
    /// <response code="401">Chua xac thuc (khong co token hop le).</response>
    /// <response code="500">Loi he thong khi xuat metric.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            var prometheusText = await _metricService.ExportPrometheusFormatAsync(cancellationToken);
            return Content(prometheusText, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi xuat metric.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = "error",
                code = "METRICS_EXPORT_FAILED",
                message = "Loi he thong khi xuat metric."
            });
        }
    }

    /// <summary>
    /// Lay mot metric theo ten.
    /// </summary>
    /// <param name="name">Ten chi so (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining).</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    /// <returns>Gia tri chi so.</returns>
    /// <response code="200">Tra ve gia tri metric.</returns>
    /// <response code="404">Khong tim thay metric.</response>
    /// <response code="401">Chua xac thuc.</response>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> GetMetric(string name, CancellationToken cancellationToken = default)
    {
        var metric = await _metricService.GetMetricAsync(name, cancellationToken);
        if (metric == null)
            return NotFound(new { status = "error", code = "METRIC_NOT_FOUND", message = $"Khong tim thay metric '{name}'." });

        return Ok(new { name = metric.Name, value = metric.Value, unit = metric.Unit, collectedAt = metric.CollectedAt });
    }

    /// <summary>
    /// Cap nhat gia tri mot metric (dung cho test hoac Admin cap nhat thu cong).
    /// </summary>
    /// <param name="name">Ten chi so.</param>
    /// <param name="value">Gia tri chi so.</param>
    /// <param name="unit">Don vi tinh.</param>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    /// <returns>Ket qua cap nhat.</returns>
    /// <response code="200">Da cap nhat metric.</response>
    /// <response code="401">Chua xac thuc.</response>
    [HttpPost("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> SetMetric(string name, [FromBody] SetMetricRequest request, CancellationToken cancellationToken = default)
    {
        await _metricService.SetMetricAsync(name, request.Value, request.Unit, cancellationToken);
        return Ok(new { status = "success", code = "METRIC_UPDATED", message = $"Da cap nhat metric '{name}'." });
    }
}

/// <summary>
/// Request cap nhat gia tri metric.
/// </summary>
public record SetMetricRequest(double Value, string Unit);

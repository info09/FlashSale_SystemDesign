using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quan ly HealthCheck cua he thong Flash Sale.
/// Kiem tra tinh trang ket noi den Redis, RabbitMQ, PostgreSQLPrimary (FR-06, NFR-07).
/// Thuat ngua chuan theo docs/domain-glossary.md: HealthCheck, ApiGateway, Redis, RabbitMQ, PostgreSQLPrimary.
/// Khong duoc goi la: HealthApi, StatusController, PingController.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/health")]
[ApiExplorerSettings(GroupName = "v1")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IHealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Kiem tra tinh trang suc khoe cua cac dich vao: PostgreSQLPrimary, Redis, RabbitMQ.
    /// Tra ve 200 OK neu tat ca dich vao khong khoe, 503 ServiceUnavailable neu co it nhat mot dich vao loi.
    /// </summary>
    /// <param name="cancellationToken">Token huy bo thao tac bat dong bo.</param>
    /// <returns>Tinh trang suc khoe cua he thong va tung dich vao.</returns>
    /// <response code="200">Tat ca dich vao khong khoe.</response>
    /// <response code="401">Chua xac thuc (khong co token hop le).</response>
    /// <response code="503">Co it nhat mot dich vao khong khoe.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var result = await _healthCheckService.CheckAllAsync(cancellationToken);

        var statusCode = result.IsHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        _logger.LogInformation("Health check: Status={Status}, Checks={Checks}",
            result.Status, System.Text.Json.JsonSerializer.Serialize(result.Checks));

        return statusCode == StatusCodes.Status200OK
            ? Ok(new { status = result.Status, checks = result.Checks })
            : StatusCode(statusCode, new { status = result.Status, checks = result.Checks });
    }
}

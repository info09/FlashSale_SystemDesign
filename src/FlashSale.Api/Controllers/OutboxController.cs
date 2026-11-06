using FlashSale.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quan ly bang Outbox (purge/archive) cho DevOps.
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, Purge, DevOps, PostgreSQLPrimary.
/// Khong duoc goi la: OutboxApi, OutboxManagementController, PurgeController.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/outbox")]
[ApiExplorerSettings(GroupName = "v1")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxPurgeService _purgeService;
    private readonly ILogger<OutboxController> _logger;

    public OutboxController(IOutboxPurgeService purgeService, ILogger<OutboxController> logger)
    {
        _purgeService = purgeService;
        _logger = logger;
    }

    /// <summary>
    /// Kich hoat purge/archive bang Outbox ngay lap tuc (goi thu cong boi DevOps).
    /// Chi xoa cac ghi Outbox da xu ly (Published/Failed) va cu hon Outbox:PurgeDays (mac dinh 30 ngay),
    /// giu lai ghi Pending de tranh mat su kien (dong ho NFR-06).
    /// </summary>
    /// <returns>So luong ghi Outbox da duoc xoa.</returns>
    /// <response code="200">Da purge thanh cong, tra ve so luong ghi da xoa.</response>
    /// <response code="401">Chua xac thuc (khong co token hop le).</response>
    /// <response code="500">Loi he thong khi purge.</response>
    [HttpPost("purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> Purge(CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _purgeService.PurgeProcessedOutboxesAsync(cancellationToken);

            _logger.LogInformation("DevOps kich hoat purge Outbox: da xoa {DeletedCount} ghi.", deletedCount);

            return Ok(new
            {
                status = "success",
                code = "OUTBOX_PURGED",
                message = $"Da xoa {deletedCount} ghi Outbox da xu ly va cu han.",
                deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi purge Outbox.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = "error",
                code = "OUTBOX_PURGE_FAILED",
                message = "Loi he thong khi purge bang Outbox."
            });
        }
    }
}

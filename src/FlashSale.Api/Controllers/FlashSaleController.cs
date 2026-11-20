using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using OrderCreatedMessage = FlashSale.Application.Features.Inventory.Events.OrderCreatedMessage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Security.Claims;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quản lý mua hàng và giữ chỗ (Flash Sale) cho khách hàng.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, OrderPipeline, Queue, Message, Http202Accepted, Hold.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/flash-sale")]
[ApiExplorerSettings(GroupName = "v1")]
public class FlashSaleController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;
    private readonly IHoldService _holdService;

    public FlashSaleController(
        IInventoryService inventoryService,
        IRabbitMqPublisherService rabbitMqPublisherService,
        IHoldService holdService)
    {
        _inventoryService = inventoryService;
        _rabbitMqPublisherService = rabbitMqPublisherService;
        _holdService = holdService;
    }

    /// <summary>
    /// Mua hàng (đặt đơn) cho một sản phẩm (luồng nhanh hiện tại, chạy song song).
    /// Trừ kho atomic trên Redis, publish OrderCreatedMessage vào RabbitMQ Queue và trả về HTTP 202 Accepted.
    /// </summary>
    [HttpPost("buy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Buy([FromBody] BuyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            return BadRequest(new
            {
                status = "error",
                code = "INVALID_REQUEST",
                message = "ProductId không được để trống."
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INVALID_REQUEST",
                message = "Quantity phải lớn hơn 0."
            });
        }

        var clientId = GetClientId();
        if (string.IsNullOrWhiteSpace(clientId))
            return UnauthorizedError();

        var deductResult = await _inventoryService.DeductInventoryAsync(request.ProductId, request.Quantity);
        if (!deductResult.IsSuccess)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INSUFFICIENT_INVENTORY",
                message = $"Sản phẩm '{request.ProductId}' đã hết hàng (OverSelling). Không tạo đơn.",
                remainingQuantity = deductResult.RemainingQuantity
            });
        }

        var orderId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;
        var orderCreatedMessage = new OrderCreatedMessage
        {
            CorrelationId = orderId,
            MessageId = Guid.NewGuid(),
            OccurredOn = occurredOn,
            Payload = new
            {
                orderId,
                productId = request.ProductId,
                quantity = request.Quantity,
                clientId,
                orderStatus = "Created",
                createdOn = occurredOn
            }
        };

        await _rabbitMqPublisherService.PublishOrderCreatedAsync(orderCreatedMessage);

        return Accepted(new
        {
            status = "success",
            code = "ORDER_ACCEPTED",
            message = "Đơn hàng đã được nhận và đang được xử lý.",
            orderId = orderId,
            productId = request.ProductId,
            quantity = request.Quantity,
            clientId = clientId,
            orderStatus = "Created",
            createdOn = occurredOn
        });
    }

    /// <summary>
    /// Tạo hold/giữ chỗ cho sản phẩm (reserve inventory trên Redis, TTL 60s).
    /// </summary>
    [HttpPost("hold")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHold([FromBody] HoldRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ProductId))
        {
            return BadRequest(new { status = "error", code = "INVALID_REQUEST", message = "ProductId không được để trống." });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new { status = "error", code = "INVALID_REQUEST", message = "Quantity phải lớn hơn 0." });
        }

        var clientId = GetClientId();
        if (string.IsNullOrWhiteSpace(clientId))
            return UnauthorizedError();

        var result = await _holdService.CreateHoldAsync(new CreateHoldCommand(request.ProductId, request.Quantity, clientId));

        return Ok(new
        {
            status = "success",
            code = "HOLD_CREATED",
            holdId = result.HoldId,
            productId = request.ProductId,
            quantity = request.Quantity,
            expiresAt = result.ExpiresAt,
            ttlSeconds = result.TtlSeconds
        });
    }

    /// <summary>
    /// Xác nhận hold và submit thông tin khách hàng.
    /// Consume hold, lưu InformationCustomer, ghi Outbox và phát hành tạo Order (immediate + outbox fallback).
    /// </summary>
    [HttpPost("hold/{holdId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> ConfirmHold([FromRoute] Guid holdId, [FromBody] ConfirmHoldRequest request)
    {
        if (request is null)
            return BadRequest(new { status = "error", code = "INVALID_REQUEST", message = "Thông tin xác nhận không hợp lệ." });

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new { status = "error", code = "INVALID_REQUEST", message = "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ." });
        }

        var clientId = GetClientId();
        if (string.IsNullOrWhiteSpace(clientId))
            return UnauthorizedError();

        var result = await _holdService.ConfirmHoldAsync(new ConfirmHoldCommand(
            holdId,
            clientId,
            request.Name,
            request.PhoneNumber,
            request.Address));

        return Accepted(new
        {
            status = "success",
            code = "ORDER_ACCEPTED",
            message = "Đơn hàng đã được nhận và đang được xử lý.",
            holdId = result.HoldId,
            orderId = result.OrderId,
            correlationId = result.CorrelationId,
            productId = result.ProductId,
            quantity = result.Quantity
        });
    }

    /// <summary>
    /// Hủy/release hold (idempotent), trả inventory/slot.
    /// </summary>
    [HttpPost("hold/{holdId:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> ReleaseHold([FromRoute] Guid holdId)
    {
        var clientId = GetClientId();
        if (string.IsNullOrWhiteSpace(clientId))
            return UnauthorizedError();

        var result = await _holdService.ReleaseHoldAsync(new ReleaseHoldCommand(holdId, clientId));

        return Ok(new
        {
            status = "success",
            code = "HOLD_RELEASED",
            holdId,
            released = result.Released
        });
    }

    private string? GetClientId()
    {
        return User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;
    }

    private IActionResult UnauthorizedError()
    {
        return Unauthorized(new
        {
            status = "error",
            code = "UNAUTHORIZED",
            message = "Không xác định được khách hàng."
        });
    }
}

/// <summary>
/// Request yêu cầu mua hàng (luồng nhanh).
/// </summary>
public record BuyRequest(string ProductId, int Quantity);

/// <summary>
/// Request tạo hold/giữ chỗ.
/// </summary>
public record HoldRequest(string ProductId, int Quantity);

/// <summary>
/// Request xác nhận hold với thông tin khách hàng.
/// </summary>
public record ConfirmHoldRequest(string Name, string PhoneNumber, string Address);

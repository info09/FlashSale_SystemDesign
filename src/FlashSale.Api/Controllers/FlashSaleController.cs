using FlashSale.Application.Features.Order.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Security.Claims;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quản lý mua hàng (Flash Sale) cho khách hàng.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, OrderPipeline, Queue, Message, Http202Accepted.
/// Không được gọi là: FlashSaleApi, PurchaseController, BuyController, GatewayController.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/flash-sale")]
[ApiExplorerSettings(GroupName = "v1")]
public class FlashSaleController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;

    public FlashSaleController(IInventoryService inventoryService, IRabbitMqPublisherService rabbitMqPublisherService)
    {
        _inventoryService = inventoryService;
        _rabbitMqPublisherService = rabbitMqPublisherService;
    }

    /// <summary>
    /// Mua hàng (đặt đơn) cho một sản phẩm.
    /// Trừ kho atomic trên Redis, sau đó đẩy thông tin tạo đơn vào RabbitMQ Queue và trả về HTTP 202 Accepted ngay cho client.
    /// Order + Outbox được ghi vào PostgreSQLPrimary bởi Async Worker (không ghi tại API).
    /// </summary>
    /// <param name="request">Thông tin mua hàng: ProductId và Quantity.</param>
    /// <returns>Kết quả mua hàng: thành công (202 Accepted) hoặc hết hàng (400).</returns>
    /// <response code="202">Đã nhận đơn hàng và đang xử lý.</response>
    /// <response code="400">Hết kho (OverSelling) hoặc yêu cầu không hợp lệ.</response>
    /// <response code="401">Chưa xác thực (không có token hợp lệ).</response>
    [HttpPost("buy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> Buy([FromBody] BuyRequest request)
    {
        // Validate input trước khi gọi service
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

        // Lấy ClientId từ authenticated user (ClaimTypes.NameIdentifier)
        var clientId = User.Identity?.IsAuthenticated == true
            ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Unauthorized(new
            {
                status = "error",
                code = "UNAUTHORIZED",
                message = "Không xác định được khách hàng."
            });
        }

        // Trừ kho atomic trên Redis (ngoài transaction DB)
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

        // Tạo message tạo đơn (chưa lưu Order vào PostgreSQL tại API)
        var orderId = Guid.NewGuid();
        var orderCreatedMessage = OrderCreatedMessage.Create(
            new Order(orderId, request.ProductId, request.Quantity, clientId),
            orderId);

        // Publish message vào RabbitMQ Queue (OrderPipeline)
        await _rabbitMqPublisherService.PublishOrderCreatedAsync(orderCreatedMessage);

        // Trả về HTTP 202 Accepted ngay (không chờ xử lý hoàn tất)
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
            createdOn = orderCreatedMessage.OccurredOn
        });
    }
}

/// <summary>
/// Request yêu cầu mua hàng.
/// </summary>
public record BuyRequest(string ProductId, int Quantity);

using FlashSale.Application.Features.Order.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
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
    private readonly IOrderService _orderService;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;

    public FlashSaleController(IOrderService orderService, IRabbitMqPublisherService rabbitMqPublisherService)
    {
        _orderService = orderService;
        _rabbitMqPublisherService = rabbitMqPublisherService;
    }

    /// <summary>
    /// Mua hàng (đặt đơn) cho một sản phẩm.
    /// Sau khi trừ kho thành công, đẩy thông tin tạo đơn vào RabbitMQ Queue và trả về HTTP 202 Accepted ngay cho client.
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

        // Tạo đơn hàng (trừ kho thành công thì tạo đơn)
        var order = await _orderService.CreateOrderAsync(request.ProductId, request.Quantity, clientId);

        // Publish message vào RabbitMQ Queue (OrderPipeline)
        var orderCreatedMessage = OrderCreatedMessage.Create(order, order.Id);
        await _rabbitMqPublisherService.PublishOrderCreatedAsync(orderCreatedMessage);

        // Trả về HTTP 202 Accepted ngay (không chờ xử lý hoàn tất)
        return Accepted(new
        {
            status = "success",
            code = "ORDER_ACCEPTED",
            message = "Đơn hàng đã được nhận và đang được xử lý.",
            orderId = order.Id,
            productId = order.ProductId,
            quantity = order.Quantity,
            clientId = order.ClientId,
            orderStatus = order.OrderStatus,
            createdOn = order.CreatedOn
        });
    }
}

/// <summary>
/// Request yêu cầu mua hàng.
/// </summary>
public record BuyRequest(string ProductId, int Quantity);

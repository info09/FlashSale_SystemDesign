using FlashSale.Application.Features.Inventory.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quản lý Inventory (tồn kho) cho Flash Sale.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading, Product.
/// Không được gọi là: StockController, TồnKhoController, KhoController.
/// </summary>
[ApiController]
[Route("api/v1/inventory")]
[ApiExplorerSettings(GroupName = "v1")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Trừ kho atomic cho một sản phẩm (AtomicInventoryOffloading).
    /// Nếu không đủ kho, trả về HTTP 400 không tạo đơn (FR-02).
    /// </summary>
    /// <param name="request">Thông tin trừ kho: ProductId và QuantityToDeduct.</param>
    /// <returns>Kết quả trừ kho: thành công hoặc hết hàng.</returns>
    /// <response code="200">Trừ kho thành công.</response>
    /// <response code="400">Hết kho (OverSelling), không tạo đơn.</response>
    /// <response code="400">Yêu cầu không hợp lệ (ProductId trống hoặc QuantityToDeduct &lt;= 0).</response>
    [HttpPost("deduct")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> DeductInventory([FromBody] DeductInventoryRequest request)
    {
        // Validate input trước khi gọi mediator
        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            return BadRequest(new
            {
                status = "error",
                code = "INVALID_REQUEST",
                message = "ProductId không được để trống."
            });
        }

        if (request.QuantityToDeduct <= 0)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INVALID_REQUEST",
                message = "QuantityToDeduct phải lớn hơn 0."
            });
        }

        var command = new DeductInventoryCommand(request.ProductId, request.QuantityToDeduct);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INSUFFICIENT_INVENTORY",
                message = $"Sản phẩm '{request.ProductId}' đã hết hàng (OverSelling). Không tạo đơn.",
                remainingQuantity = result.RemainingQuantity
            });
        }

        return Ok(new
        {
            status = "success",
            code = "INVENTORY_DEDUCTED",
            message = $"Trừ kho thành công cho sản phẩm '{request.ProductId}'.",
            productId = request.ProductId,
            deductedQuantity = result.DeductedQuantity,
            remainingQuantity = result.RemainingQuantity
        });
    }
}

/// <summary>
/// Request yêu cầu trừ kho.
/// </summary>
public record DeductInventoryRequest(string ProductId, int QuantityToDeduct);

using FlashSale.Application.Features.Inventory.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quản lý Inventory (tồn kho) cho Flash Sale.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading, Product.
/// Không được gọi là: StockController, TồnKhoController, KhoController.
/// </summary>
[Authorize]
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
    /// Thêm tồn kho (restock) cho một sản phẩm.
    /// Nếu sản phẩm chưa có trong Redis, sẽ tạo mới với số lượng đã thêm.
    /// </summary>
    /// <param name="request">Thông tin thêm kho: ProductId và QuantityToAdd.</param>
    /// <returns>Kết quả thêm kho: thành công hoặc lỗi.</returns>
    /// <response code="200">Thêm kho thành công.</response>
    /// <response code="400">Yêu cầu không hợp lệ (ProductId trống hoặc QuantityToAdd &lt;= 0).</response>
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> AddInventory([FromBody] AddInventoryRequest request)
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

        if (request.QuantityToAdd <= 0)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INVALID_REQUEST",
                message = "QuantityToAdd phải lớn hơn 0."
            });
        }

        var command = new AddInventoryCommand(request.ProductId, request.QuantityToAdd);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                status = "error",
                code = "INTERNAL_ERROR",
                message = $"Không thể thêm kho cho sản phẩm '{request.ProductId}'."
            });
        }

        return Ok(new
        {
            status = "success",
            code = "INVENTORY_ADDED",
            message = $"Thêm kho thành công cho sản phẩm '{request.ProductId}'.",
            productId = request.ProductId,
            addedQuantity = result.AddedQuantity,
            remainingQuantity = result.RemainingQuantity
        });
    }
}

/// <summary>
/// Request yêu cầu trừ kho.
/// </summary>
public record DeductInventoryRequest(string ProductId, int QuantityToDeduct);

/// <summary>
/// Request yêu cầu thêm tồn kho.
/// </summary>
public record AddInventoryRequest(string ProductId, int QuantityToAdd);

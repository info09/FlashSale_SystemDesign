using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ Inventory ở lớp Application, orchestrates việc trừ kho qua Domain Repository.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading, InsufficientInventoryException.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryService(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>
    /// Trừ kho atomic cho một sản phẩm.
    /// Nếu không đủ kho, trả về DeductInventoryResult.IsSuccess = false (không ném exception, middleware sẽ xử lý trả HTTP 400).
    /// </summary>
    public async Task<DeductInventoryResult> DeductInventoryAsync(string productId, int quantityToDeduct, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("ProductId không được để trống", nameof(productId));
        }

        if (quantityToDeduct <= 0)
        {
            throw new ArgumentException("QuantityToDeduct phải lớn hơn 0", nameof(quantityToDeduct));
        }

        // Gọi Domain Repository để thực hiện trừ kho atomic qua Redis Lua Script
        var remainingQuantity = await _inventoryRepository.DeductInventoryAsync(productId, quantityToDeduct, cancellationToken);

        if (remainingQuantity == null)
        {
            // Không đủ kho, trả về kết quả bị chặn (không ném exception để middleware xử lý trả HTTP 400)
            return new DeductInventoryResult(
                IsSuccess: false,
                RemainingQuantity: 0,
                DeductedQuantity: 0);
        }

        return new DeductInventoryResult(
            IsSuccess: true,
            RemainingQuantity: remainingQuantity.Value,
            DeductedQuantity: quantityToDeduct);
    }
}

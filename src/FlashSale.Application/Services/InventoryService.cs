using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
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
    /// Thêm tồn kho (restock) cho một sản phẩm.
    /// Trả về số lượng tồn kho sau khi thêm.
    /// </summary>
    public async Task<AddInventoryResult> AddInventoryAsync(string productId, int quantityToAdd, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("ProductId không được để trống", nameof(productId));
        }

        if (quantityToAdd <= 0)
        {
            throw new ArgumentException("QuantityToAdd phải lớn hơn 0", nameof(quantityToAdd));
        }

        // Lấy số lượng tồn kho hiện tại từ Redis
        var currentQuantity = await _inventoryRepository.GetInventoryAsync(productId, cancellationToken);

        // Tính toán số lượng tồn kho mới
        var newQuantity = currentQuantity.GetValueOrDefault() + quantityToAdd;

        // Cập nhật số lượng tồn kho mới vào Redis
        await _inventoryRepository.SetInventoryAsync(productId, newQuantity, cancellationToken);

        return new AddInventoryResult(
            IsSuccess: true,
            RemainingQuantity: newQuantity,
            AddedQuantity: quantityToAdd);
    }
}

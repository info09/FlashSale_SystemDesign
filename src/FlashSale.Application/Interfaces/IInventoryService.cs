using FlashSale.Application.Features.Inventory.Commands;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ Inventory ở lớp Application.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Trừ kho atomic cho một sản phẩm.
    /// Nếu không đủ kho, trả về DeductInventoryResult.IsSuccess = false.
    /// </summary>
    Task<DeductInventoryResult> DeductInventoryAsync(string productId, int quantityToDeduct, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm tồn kho (restock) cho một sản phẩm.
    /// Trả về số lượng tồn kho sau khi thêm.
    /// </summary>
    Task<AddInventoryResult> AddInventoryAsync(string productId, int quantityToAdd, CancellationToken cancellationToken = default);
}

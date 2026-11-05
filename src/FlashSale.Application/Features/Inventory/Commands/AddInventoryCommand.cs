using MediatR;

namespace FlashSale.Application.Features.Inventory.Commands;

/// <summary>
/// Command yêu cầu thêm tồn kho (restock) cho một sản phẩm.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, Product.
/// </summary>
public record AddInventoryCommand(string ProductId, int QuantityToAdd) : IRequest<AddInventoryResult>;

/// <summary>
/// Kết quả trả về sau khi thêm tồn kho.
/// </summary>
public record AddInventoryResult(bool IsSuccess, int RemainingQuantity, int AddedQuantity);

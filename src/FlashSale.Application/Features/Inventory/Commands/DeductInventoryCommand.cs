using MediatR;

namespace FlashSale.Application.Features.Inventory.Commands;

/// <summary>
/// Command yêu cầu trừ kho (AtomicInventoryOffloading) cho một sản phẩm.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading, Product.
/// </summary>
public record DeductInventoryCommand(string ProductId, int QuantityToDeduct, string ClientId) : IRequest<DeductInventoryResult>;

/// <summary>
/// Kết quả trả về sau khi trừ kho.
/// </summary>
public record DeductInventoryResult(bool IsSuccess, int RemainingQuantity, int DeductedQuantity);

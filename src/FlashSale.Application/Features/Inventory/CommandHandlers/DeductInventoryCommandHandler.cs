using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Inventory.CommandHandlers;

/// <summary>
/// CommandHandler xử lý DeductInventoryCommand, delegate sang IInventoryService.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading.
/// </summary>
public class DeductInventoryCommandHandler : IRequestHandler<DeductInventoryCommand, DeductInventoryResult>
{
    private readonly IInventoryService _inventoryService;

    public DeductInventoryCommandHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<DeductInventoryResult> Handle(DeductInventoryCommand request, CancellationToken cancellationToken)
    {
        return await _inventoryService.DeductInventoryAsync(
            request.ProductId,
            request.QuantityToDeduct,
            cancellationToken);
    }
}

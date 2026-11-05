using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Inventory.CommandHandlers;

public class AddInventoryCommandHandler : IRequestHandler<AddInventoryCommand, AddInventoryResult>
{
    private readonly IInventoryService _inventoryService;
    public AddInventoryCommandHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }
    public async Task<AddInventoryResult> Handle(AddInventoryCommand request, CancellationToken cancellationToken)
    {
        return await _inventoryService.AddInventoryAsync(
            request.ProductId,
            request.QuantityToAdd,
            cancellationToken);
    }
}

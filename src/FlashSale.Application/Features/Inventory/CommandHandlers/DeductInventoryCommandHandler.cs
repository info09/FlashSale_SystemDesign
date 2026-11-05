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
    private readonly IOrderService _orderService;

    public DeductInventoryCommandHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<DeductInventoryResult> Handle(DeductInventoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(request.ProductId, request.Quantity, request.ClientId, cancellationToken);

        return new DeductInventoryResult(result.Id, result.OrderStatus, result.CreatedOn);
    }
}

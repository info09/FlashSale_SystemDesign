using FlashSale.Application.Features.Order.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Order.CommandHandlers;

/// <summary>
/// CommandHandler xử lý CreateOrderCommand, delegate sang IOrderService.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, AtomicInventoryOffloading.
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderService _orderService;

    public CreateOrderCommandHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateOrderAsync(
            request.ProductId,
            request.Quantity,
            request.ClientId,
            cancellationToken);

        return new CreateOrderResult(
            OrderId: order.Id,
            OrderStatus: order.OrderStatus,
            CreatedOn: order.CreatedOn);
    }
}

using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ Order ở lớp Application, orchestrates việc tạo đơn hàng qua Domain Repository.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, OrderCreatedMessage, AtomicInventoryOffloading.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public OrderService(IOrderRepository orderRepository, IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>
    /// Tạo đơn hàng mới sau khi kiểm tra và trừ kho thành công.
    /// </summary>
    public async Task<Order> CreateOrderAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId không được để trống", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0", nameof(quantity));

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId không được để trống", nameof(clientId));

        // Kiểm tra tồn kho trước khi tạo đơn
        var remainingQuantity = await _inventoryRepository.DeductInventoryAsync(productId, quantity, cancellationToken);

        if (remainingQuantity == null)
        {
            throw new InvalidOperationException($"Sản phẩm '{productId}' đã hết hàng (OverSelling). Không tạo đơn.");
        }

        // Tạo đơn hàng
        var order = new Order(productId, quantity, clientId);
        await _orderRepository.AddAsync(order, cancellationToken);

        return order;
    }
}

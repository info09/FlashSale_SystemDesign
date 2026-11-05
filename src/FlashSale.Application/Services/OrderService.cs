using System.Text.Json;
using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ Order ở lớp Application, orchestrates việc tạo đơn hàng qua Domain Repository.
/// Trien khai Transactional Outbox Pattern (FR-04, NFR-06): ghi Order va Outbox trong cung 1 transaction
/// PostgreSQLPrimary, dam bao khong mat su kien da nhan 202 Accepted.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, OrderCreatedMessage, AtomicInventoryOffloading, Outbox, TransactionalOutboxPattern.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Tạo đơn hàng mới sau khi kiểm tra và trừ kho thành công.
    /// Order va su kien Outbox duoc ghi trong cung 1 transaction PostgreSQLPrimary.
    /// </summary>
    public async Task<Order> CreateOrderAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId không được để trống", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0", nameof(quantity));

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId không được để trống", nameof(clientId));

        // Kiểm tra tồn kho trước khi tạo đơn (trên Redis, ngoài transaction DB)
        var remainingQuantity = await _inventoryRepository.DeductInventoryAsync(productId, quantity, cancellationToken);

        if (remainingQuantity == null)
        {
            throw new InvalidOperationException($"Sản phẩm '{productId}' đã hết hàng (OverSelling). Không tạo đơn.");
        }

        // Bat dau transaction PostgreSQLPrimary de ghi Order + Outbox cung luc (Transactional Outbox Pattern)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Tạo đơn hàng
            var order = new Order(productId, quantity, clientId);
            await _orderRepository.AddAsync(order, cancellationToken);

            // Ghi su kien vao Outbox (cung transaction voi Order)
            var orderCreatedMessage = OrderCreatedMessage.Create(order, order.Id);
            var payload = JsonSerializer.Serialize(orderCreatedMessage);
            await _outboxService.AddOutboxAsync(
                order.Id,
                "OrderCreated",
                payload,
                order.Id,
                cancellationToken);

            // Commit transaction: luu Order + Outbox trong cung 1 transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return order;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

using FlashSale.Domain.Entities;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ Order ở lớp Application.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order, IOrderService.
/// Khong duoc goi là: IOrderDao, IOrderManager.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Tạo đơn hàng mới sau khi trừ kho thành công.
    /// Trả về Order đã tạo.
    /// </summary>
    Task<Order> CreateOrderAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default);
}

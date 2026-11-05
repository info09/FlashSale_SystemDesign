using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dinh nghia thao tac truy cap Order trong kho du lieu (PostgreSQLPrimary).
/// O lop Domain chi dinh nghia contract, chua viet logic thuc thi.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Order, IOrderRepository.
/// Khong duoc goi là: IOrderDao, IOrderService, IOrderStore.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Lay Order theo Id.
    /// </summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay Order theo ClientId.
    /// </summary>
    Task<Order?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Them Order moi vao kho du lieu.
    /// </summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat trang thai Order.
    /// </summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

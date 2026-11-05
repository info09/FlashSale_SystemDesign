using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IOrderRepository dung PostgreSQLPrimary.
/// Map giua Order domain entity va bang Order trong DB.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Order, IOrderRepository, PostgreSQLPrimary.
/// Khong duoc goi la: IOrderDao, IOrderService, IOrderStore.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly PostgreSQLPrimaryContext _context;

    public OrderRepository(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Order?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.ClientId == clientId, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
    }
}

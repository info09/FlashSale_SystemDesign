using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Triển khai <see cref="IInformationCustomerRepository"/> dùng PostgreSQLPrimary.
/// Lưu và truy vấn thông tin khách hàng gắn với Hold.
/// </summary>
public class InformationCustomerRepository : IInformationCustomerRepository
{
    private readonly PostgreSQLPrimaryContext _context;

    /// <summary>
    /// Khởi tạo repository với DbContext PostgreSQLPrimary.
    /// </summary>
    /// <param name="context">DbContext chính của hệ thống.</param>
    public InformationCustomerRepository(PostgreSQLPrimaryContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Thêm bản ghi <see cref="InformationCustomer"/> vào DbContext (chưa commit).
    /// </summary>
    /// <param name="informationCustomer">Thông tin khách hàng cần lưu.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    public async Task AddAsync(InformationCustomer informationCustomer, CancellationToken cancellationToken = default)
    {
        await _context.InformationCustomers.AddAsync(informationCustomer, cancellationToken);
    }

    /// <summary>
    /// Lấy thông tin khách hàng theo HoldId.
    /// </summary>
    /// <param name="holdId">Id của hold.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Thông tin khách hàng nếu tồn tại; ngược lại null.</returns>
    public async Task<InformationCustomer?> GetByHoldIdAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        return await _context.InformationCustomers
            .FirstOrDefaultAsync(x => x.HoldId == holdId, cancellationToken);
    }
}

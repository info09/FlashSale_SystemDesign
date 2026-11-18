using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao diện thao tác lưu trữ InformationCustomer (PostgreSQL).
/// Thuật ngữ chuẩn: InformationCustomer, Hold.
/// </summary>
public interface IInformationCustomerRepository
{
    Task AddAsync(InformationCustomer informationCustomer, CancellationToken cancellationToken = default);
    Task<InformationCustomer?> GetByHoldIdAsync(Guid holdId, CancellationToken cancellationToken = default);
}

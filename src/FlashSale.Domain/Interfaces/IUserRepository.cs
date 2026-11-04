using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dinh nghia thao tac truy cap User trong kho du lieu (PostgreSQLPrimary).
/// O lop Domain chi dinh nghia contract, chua viet logic thuc thi.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, IUserRepository.
/// Khong duoc goi la: IUserDao, IUserService, IAccountRepository.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Lay User theo UserName.
    /// </summary>
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay User theo Id.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Them User moi vao kho du lieu.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra UserName da ton tai chua.
    /// </summary>
    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Email da ton tai chua.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

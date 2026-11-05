using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dinh nghia thao tac truy cap User profile trong kho du lieu (PostgreSQLPrimary).
/// Mat khau va xac thuc duoc ASP.NET Core Identity xu ly, khong luu trong Domain.
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
    /// Them User profile moi (UserName, Email) vao kho du lieu.
    /// Mat khau duoc luu boi ASP.NET Core Identity, khong o day.
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

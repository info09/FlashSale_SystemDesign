using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dich vu User o lop Domain (business logic dang nhap, dang ki).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, IUserService.
/// Khong duoc goi la: IAccountService, IAuthService, ICustomerService.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Dang ki nguoi dung moi. Tra ve User da tao neu thanh cong.
    /// </summary>
    Task<User> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xac thuc nguoi dung (UserName + Password). Tra ve User neu xac thuc thanh cong, neu that bai tra ve null.
    /// </summary>
    Task<User?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
}

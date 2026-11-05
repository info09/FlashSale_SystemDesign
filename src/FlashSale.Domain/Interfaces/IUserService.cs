using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dich vu User o lop Domain (business logic dang nhap, dang ki).
/// Mat khau chi duoc truyen qua string (khong luu hash o Domain), xac thuc do ASP.NET Core Identity xu ly o Infrastructure.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, IUserService.
/// Khong duoc goi la: IAccountService, IAuthService, ICustomerService.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Dang ki nguoi dung moi. Mat khau duoc truyen qua string, duoc ASP.NET Core Identity hash va luu o Infrastructure.
    /// Tra ve User profile da tao neu thanh cong.
    /// </summary>
    Task<User> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xac thuc nguoi dung (UserName + Password). Mat khau duoc ASP.NET Core Identity verify o Infrastructure.
    /// Tra ve User profile neu xac thuc thanh cong, neu that bai tra ve null.
    /// </summary>
    Task<User?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
}

using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IUserService o Infrastructure, su dung ASP.NET Core Identity
/// (UserManager + SignInManager) de dang ki va xac thuc nguoi dung.
/// Mat khau duoc Identity hash va luu trong PostgreSQLPrimary, khong luu o Domain.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, IUserService, PostgreSQLPrimary.
/// Khong duoc goi la: IAccountService, IAuthService, ICustomerService.
/// </summary>
public class AuthService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserRepository _userRepository;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserRepository userRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userRepository = userRepository;
    }

    public async Task<User> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.UserNameExistsAsync(userName, cancellationToken))
            throw new InvalidOperationException($"UserName '{userName}' da ton tai.");

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            throw new InvalidOperationException($"Email '{email}' da ton tai.");

        var user = new User(userName, email);
        var appUser = new ApplicationUser(userName, email);
        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Khong the them User voi mat khau: {errors}");
        }

        return user;
    }

    public async Task<User?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByNameAsync(userName);
        if (appUser == null)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(appUser, password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return null;

        return await _userRepository.GetByUserNameAsync(userName, cancellationToken);
    }
}

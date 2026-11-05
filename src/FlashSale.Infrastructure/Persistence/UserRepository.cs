using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Trien khai IUserRepository dung ASP.NET Core Identity + PostgreSQLPrimary.
/// Map giua User domain entity (profile) va ApplicationUser (IdentityUser).
/// Mat khau duoc ASP.NET Core Identity hash va luu, khong o Domain.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, IUserRepository, PostgreSQLPrimary.
/// Khong duoc goi la: IUserDao, IUserService, IAccountRepository.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByNameAsync(userName);
        return MapToDomain(appUser);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(id.ToString());
        return MapToDomain(appUser);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var appUser = new ApplicationUser(user.UserName, user.Email);
        var result = await _userManager.CreateAsync(appUser);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Khong the them User profile: {errors}");
        }
    }

    /// <summary>
    /// Tao User voi mat khau (duoc ASP.NET Core Identity hash).
    /// </summary>
    public async Task CreateWithPasswordAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var appUser = new ApplicationUser(user.UserName, user.Email);
        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Khong the them User voi mat khau: {errors}");
        }
    }

    public async Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByNameAsync(userName) is not null;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    /// <summary>
    /// Xac thuc mat khau qua ASP.NET Core Identity (CheckPasswordAsync).
    /// </summary>
    public async Task<bool> CheckPasswordAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByNameAsync(user.UserName);
        if (appUser == null)
            return false;

        return await _userManager.CheckPasswordAsync(appUser, password);
    }

    private static User? MapToDomain(ApplicationUser? appUser)
    {
        if (appUser == null)
            return null;

        return new User(appUser.Id, appUser.UserName!, appUser.Email!);
    }
}

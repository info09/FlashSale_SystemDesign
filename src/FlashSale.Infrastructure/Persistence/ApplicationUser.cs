using Microsoft.AspNetCore.Identity;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Identity user class cho ASP.NET Core Identity, luu thong tin dang nhap trong PostgreSQLPrimary.
/// Map tu User domain entity (FlashSale.Domain.Entities.User).
/// Ten bang: user (snake_case theo docs/conventions.md).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, PostgreSQLPrimary.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
    }

    public ApplicationUser(string userName, string email) : base(userName)
    {
        Email = email;
        NormalizedEmail = email?.ToUpperInvariant();
        NormalizedUserName = userName?.ToUpperInvariant();
    }
}

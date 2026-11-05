using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao dinh dich vu tao JWT token de xac thuc client.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, BearerToken (jwt token).
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Ky JWT token cho nguoi dung da xac thuc thanh cong.
    /// </summary>
    Task<string> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);
}

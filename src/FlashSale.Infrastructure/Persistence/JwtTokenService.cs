using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Dich vu ky JWT token dung Bearer authentication (FR-01, NFR-04).
/// Tao access token chi voi thong tin nguoi dung (UserId, UserName, Email).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, BearerToken.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateTokenAsync(User user)
    {
        var secretKey = _configuration["JWT_SECRET"] ?? "FlashSaleSecretKey123456789012345678901234567890";
        var issuer = _configuration["JWT_ISSUER"] ?? "FlashSale.Api";
        var audience = _configuration["JWT_AUDIENCE"] ?? "FlashSale.Client";
        var expirationMinutes = int.Parse(_configuration["JWT_EXPIRATION_MINUTES"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

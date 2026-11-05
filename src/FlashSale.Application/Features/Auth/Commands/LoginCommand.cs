using MediatR;

namespace FlashSale.Application.Features.Auth.Commands;

/// <summary>
/// Command yêu cầu đăng nhập client (xac thuc qua Bearer Token/JWT).
/// Tra ve Access Token neu xac thuc thanh cong (FR-01, NFR-04).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, BearerToken.
/// </summary>
public record LoginCommand(string UserName, string Password) : IRequest<LoginResult>;

/// <summary>
/// Ket tra ve sau khi dang nhap thanh cong.
/// </summary>
public record LoginResult(string AccessToken, string TokenType, Guid UserId, string UserName, string Email, DateTimeOffset ExpiresAt);

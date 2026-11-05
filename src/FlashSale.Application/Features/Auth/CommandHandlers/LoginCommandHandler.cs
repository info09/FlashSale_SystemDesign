using FlashSale.Application.Features.Auth.Commands;
using FlashSale.Domain.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Auth.CommandHandlers;

/// <summary>
/// CommandHandler xu ly LoginCommand, goi IUserService de xac thuc
/// va IJwtTokenService de ky Access Token (FR-01, NFR-04).
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(IUserService userService, IJwtTokenService jwtTokenService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.AuthenticateAsync(request.UserName, request.Password, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("UserName hoac Password khong chinh xac.");
        }

        var accessToken = _jwtTokenService.GenerateTokenAsync(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60);

        return new LoginResult(
            AccessToken: accessToken,
            TokenType: "Bearer",
            UserId: user.Id,
            UserName: user.UserName,
            Email: user.Email,
            ExpiresAt: expiresAt);
    }
}

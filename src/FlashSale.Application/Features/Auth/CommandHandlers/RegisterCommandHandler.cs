using FlashSale.Application.Features.Auth.Commands;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Auth.CommandHandlers;

/// <summary>
/// CommandHandler xu ly RegisterCommand, goi IUserService de dang ki tai khoan moi (FR-01, NFR-04).
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, User>
{
    private readonly IUserService _userService;

    public RegisterCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<User> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _userService.RegisterAsync(request.UserName, request.Email, request.Password, cancellationToken);
    }
}

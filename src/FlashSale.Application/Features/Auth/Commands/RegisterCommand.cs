using FlashSale.Domain.Entities;
using MediatR;

namespace FlashSale.Application.Features.Auth.Commands;

/// <summary>
/// Command yêu dang ki tai khoan moi (dang ky FlashSaleShopper).
/// Tra ve User profile neu dang ki thanh cong (FR-01, NFR-04).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User.
/// </summary>
public record RegisterCommand(string UserName, string Email, string Password) : IRequest<User>;

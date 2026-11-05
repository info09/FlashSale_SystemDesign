using FlashSale.Application.Features.Auth.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Auth.Validators;

/// <summary>
/// Validator cho LoginCommand su dung FluentValidation.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("UserName không được để trống");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password không được để trống")
            .MinimumLength(6)
            .WithMessage("Password phải ít nhất 6 ký tự");
    }
}

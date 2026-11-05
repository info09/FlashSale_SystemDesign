using FlashSale.Application.Features.Auth.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Auth.Validators;

/// <summary>
/// Validator cho RegisterCommand su dung FluentValidation.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("UserName không được để trống")
            .MinimumLength(3)
            .WithMessage("UserName phải ít nhất 3 ký tự");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email không được để trống")
            .EmailAddress()
            .WithMessage("Email không hợp lệ");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password không được để trống")
            .MinimumLength(6)
            .WithMessage("Password phải ít nhất 6 ký tự");
    }
}

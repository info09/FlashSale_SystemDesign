using FluentValidation;
using FlashSale.Application.Features.RateLimit.Commands;

namespace FlashSale.Application.Features.RateLimit.Validators;

/// <summary>
/// Validator cho RateLimitCommand sử dụng FluentValidation.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit.
/// </summary>
public class RateLimitCommandValidator : AbstractValidator<RateLimitCommand>
{
    public RateLimitCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("ClientId không được để trống");

        RuleFor(x => x.MaxRequests)
            .GreaterThan(0)
            .WithMessage("MaxRequests phải lớn hơn 0");

        RuleFor(x => x.WindowSeconds)
            .GreaterThan(0)
            .WithMessage("WindowSeconds phải lớn hơn 0");
    }
}

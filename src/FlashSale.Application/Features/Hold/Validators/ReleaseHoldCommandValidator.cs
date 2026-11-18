using FlashSale.Application.Features.Hold.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Hold.Validators;

public class ReleaseHoldCommandValidator : AbstractValidator<ReleaseHoldCommand>
{
    public ReleaseHoldCommandValidator()
    {
        RuleFor(x => x.HoldId).NotEmpty().WithMessage("HoldId không được để trống");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId không được để trống");
    }
}

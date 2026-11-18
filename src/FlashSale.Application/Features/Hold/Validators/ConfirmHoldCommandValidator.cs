using FlashSale.Application.Features.Hold.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Hold.Validators;

public class ConfirmHoldCommandValidator : AbstractValidator<ConfirmHoldCommand>
{
    public ConfirmHoldCommandValidator()
    {
        RuleFor(x => x.HoldId).NotEmpty().WithMessage("HoldId không được để trống");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId không được để trống");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name không được để trống");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("PhoneNumber không được để trống");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address không được để trống");
    }
}
